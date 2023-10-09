import { getContext, setContext } from 'svelte';
import { isObject, isRedirect } from '$lib/util/types';

import type { Writable } from 'svelte/store';
import { browser } from '$app/environment';
import { ensureErrorIsTraced } from '$lib/otel';
import { getStores } from '$app/stores';

const ERROR_STORE_KEY = 'ERROR_STORE_KEY';

export function initErrorStore(error: Writable<App.Error | null>): Writable<App.Error | null> {
  setContext(ERROR_STORE_KEY, error);
  setupGlobalErrorHandlers(error);
  return error;
}

export function useError(): Writable<App.Error | null> {
  return getContext(ERROR_STORE_KEY);
}

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
  if (errorHandlersSetup) {
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
    const traceId = ensureErrorIsTraced(
      event.error,
      {event},
      {
        ['app.error.source']: handler,
        ['app.error.traced_by_handler']: true,
      }
    );
    const updateDetected = await updated.check();
    error.set({message: event.message, traceId, handler, updateDetected});
  });

  // https://developer.mozilla.org/en-US/docs/Web/API/PromiseRejectionEvent
  window.onunhandledrejection = async (event: PromiseRejectionEvent) => {
    if (isRedirect(event.reason)) {
      location.pathname = event.reason.location;
      return;
    }

    const handler = 'client-unhandledrejection';
    const message = isObject(event.reason) ? (event.reason.message as string) : undefined;
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
        ['app.error.traced_by_handler']: true,
      }
    );

    if (message?.startsWith('sendBeacon - cannot send')) {
      // The user doesn't care if a few OTEL beacons are failing
      // (see BatchSpanProcessor configuration in otel\client.ts)
      return;
    }

    const updateDetected = await updated.check();
    error.set({message: message ?? `We're not sure what happened.`, traceId, handler, updateDetected});
  };
}
