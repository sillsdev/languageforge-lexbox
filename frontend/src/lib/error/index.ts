import { browser } from '$app/environment';
import { isObject } from '$lib/util/types';
import { writable, type Writable } from 'svelte/store';

export const error: Writable<App.Error | undefined | null> = writable();

export const dismiss = (): void => error.set(null);

if (browser) {
  const { traceErrorEvent } = await import('$lib/otel/client');

  // https://developer.mozilla.org/en-US/docs/Web/API/GlobalEventHandlers/onerror#window.addEventListenererror
  window.addEventListener('error', (event: ErrorEvent) => {
    const source = 'client-error';
    const traceId = traceErrorEvent(event.error, event, { ['app.error.source']: source });
    error.set({ message: event.message, traceId, source });
  });

  // https://developer.mozilla.org/en-US/docs/Web/API/PromiseRejectionEvent
  window.onunhandledrejection = (event: PromiseRejectionEvent) => {
    const source = 'client-unhandledrejection';

    const message = isObject(event.reason) ? event.reason.message as string : undefined;
    const keysForMissingMessageError = message ? undefined
      : isObject(event.reason) ? Object.keys(event.reason).join() : undefined;
    const traceId = traceErrorEvent(event.reason, event, {
      ['app.error.source']: source,
      ['app.error.keys']: keysForMissingMessageError,
    });

    if (message?.startsWith('sendBeacon - cannot send')) {
      // The user doesn't care if a few OTEL beacons are failing
      // (see BatchSpanProcessor configuration in otel\client.ts)
      return;
    }

    error.set({ message: message ?? `We're not sure what happened.`, traceId, source });
  };
}
