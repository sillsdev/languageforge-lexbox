import { browser, dev } from '$app/environment';
import { isObject, isRedirect } from '$lib/util/types';

import { writable, type Writable } from 'svelte/store';
import { defineContext } from '$lib/util/context';
import { ensureErrorIsTraced } from '$lib/otel';
import { getStores } from '$app/stores';

export const { use: useError, init: initErrorStore } =
  defineContext<Writable<App.Error | null>, [App.Error | null]>(
    (error: App.Error | null) => writable(error),
    { onInit: setupGlobalErrorHandlers });

//we can't just have a `dismiss` function because we need to be able to call it from the template
//but we can't use `error()` after a component is created, so we need to define a hook function which is called once
//and returns a function that uses the store.
export function  useDismiss(): () => void {
  const errorStore = useError();
  return () => errorStore.set(null);
}

let errorHandlersSetup = false;
function setupGlobalErrorHandlers(error: Writable<App.Error | null>): void {
  if (!browser) {
    return;
  }
  if (errorHandlersSetup && !dev) {
    throw new Error('error handlers already setup. This should only be called once.');
  }
  errorHandlersSetup = true;

  const { updated } = getStores();

  /**
   * Errors that land in these handlers should generally already be traced. The tracing here is only a weak fallback.
   * These handlers are presumably never called in the context of a trace, so they have to create their own.
   * The metadata defined here, could be used to identify/improve those cases.
   */

  // https://developer.mozilla.org/en-US/docs/Web/API/GlobalEventHandlers/onerror#window.addEventListenererror
  // eslint-disable-next-line @typescript-eslint/no-misused-promises
  window.addEventListener('error', async (event: ErrorEvent) => {
    const handler = 'client-error';

    // Try our best to only show the user errors that are from our code
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-argument
    const showToUser = !!event.error && !errorIsFromExtension(event.error);
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment
    const eventError = event.error ?? {}; // sometimes null 🤷
    // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
    eventError.message ??= event.message; // just to make sure we don't miss anything

    const traceId = ensureErrorIsTraced(
      eventError,
      {event},
      {
        ['app.error.source']: handler,
        ['app.error.show_user']: showToUser,
      },
      !showToUser,
    );

    if (!showToUser) return;

    const updateDetected = await updated.check();
    error.set({ message: event.message, traceId, handler, updateDetected });
  });

  // https://developer.mozilla.org/en-US/docs/Web/API/PromiseRejectionEvent
  window.onunhandledrejection = async (event: PromiseRejectionEvent) => {
    if (isRedirect(event.reason)) {
      location.pathname = event.reason.location;
      return;
    }

    const handler = 'client-unhandledrejection';
    const message = isObject(event.reason) ? (event.reason.message as string) : undefined;

    // Try our best to only show the user errors that are from our code
    const showToUser = !!event.reason &&
      !errorIsFromExtension(event.reason as Error) &&
      // The user doesn't care if a few OTEL beacons are failing
      // (see BatchSpanProcessor configuration in otel\client.ts)
      !message?.startsWith('sendBeacon - cannot send');

    const keysForMissingMessageError = message
      ? undefined
      : isObject(event.reason)
        ? Object.keys(event.reason).join()
        : undefined;
    const traceId = ensureErrorIsTraced(
      event.reason,
      {event},
      {
        ['app.error.source']: handler,
        ['app.error.keys']: keysForMissingMessageError,
        ['app.error.show_user']: showToUser,
      },
      !showToUser,
    );

    if (!showToUser) return;

    const updateDetected = await updated.check();
    error.set({message: message ?? `We're not sure what happened.`, traceId, handler, updateDetected});
  };

  function errorIsFromExtension(error: Error): boolean {
    return !!error.stack &&
      // tested on Chrome, Edge, Firefox and Brave
      (error.stack.includes('chrome-extension://') || error.stack.includes('moz-extension://')
      || originatesFromAnonymousScript(error.stack));
  }

  function originatesFromAnonymousScript(stack: string): boolean {
    // VM/anonymous code errors are almost certainly not from our code
    const lastLine = stack.split('\n').pop() ?? '';
    return lastLine.includes('at <anonymous>');
  }
}
