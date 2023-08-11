import { getContext, setContext } from 'svelte';
import { isObject, isRedirect } from '$lib/util/types';

import type { Writable } from 'svelte/store';
import { browser } from '$app/environment';
import { ensureErrorIsTraced } from '$lib/otel';

const ERROR_STORE_KEY = 'ERROR_STORE_KEY';

export const initErrorStore = (error: Writable<App.Error | null>): Writable<App.Error | null> => setContext(ERROR_STORE_KEY, error);
export const error = (): Writable<App.Error | null> => getContext(ERROR_STORE_KEY);
export const dismiss = (): void => error().set(null);

export const goesToErrorPage = (error: App.Error | null): boolean => error?.handler?.endsWith('-hook') ?? false;


if (browser) {
  /**
   * Errors that land in these handlers should generally already be traced. The tracing here is only a weak fallback.
   * These handlers are presumably never called in the context of a trace, so they have to create their own.
   * The metadata defined here, could be used to identify/improve those cases.
   */

  // https://developer.mozilla.org/en-US/docs/Web/API/GlobalEventHandlers/onerror#window.addEventListenererror
  window.addEventListener('error', (event: ErrorEvent) => {
    const handler = 'client-error';
    const traceId = ensureErrorIsTraced(
      event.error,
      { event },
      {
        ['app.error.source']: handler,
        ['app.error.traced_by_handler']: true,
      }
    );
    error().set({ message: event.message, traceId, handler });
  });

  // https://developer.mozilla.org/en-US/docs/Web/API/PromiseRejectionEvent
  window.onunhandledrejection = (event: PromiseRejectionEvent) => {
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
      { event },
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

    error().set({ message: message ?? `We're not sure what happened.`, traceId, handler });
  };
}
