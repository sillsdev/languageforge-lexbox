import { browser } from '$app/environment';
import { writable, type Writable } from 'svelte/store';

export const error: Writable<App.Error | undefined | null> = writable();

export const dismiss = () => error.set(null);

if (browser) {
	const { trace_error_event } = await import('$lib/otel/client');

	// https://developer.mozilla.org/en-US/docs/Web/API/GlobalEventHandlers/onerror#window.addEventListenererror
  window.addEventListener('error', (event: ErrorEvent) => {
    const source = 'client-error';
    const traceId = trace_error_event(event.error, event, { ['app.error.source']: source });
		error.set({ message: event.message, traceId, source });
  });

	// https://developer.mozilla.org/en-US/docs/Web/API/PromiseRejectionEvent
  window.onunhandledrejection = (event: PromiseRejectionEvent) => {
    const source = 'client-unhandledrejection';
    const traceId = trace_error_event(event.reason, event, { ['app.error.source']: source });

    if (event.reason.message.startsWith('sendBeacon - cannot send')) {
      // The user doesn't care if a few OTEL beacons are failing
      // (see BatchSpanProcessor configuration in otel\client.ts)
      return;
    }

    error.set({ message: event.reason.message, traceId, source });
	};
};
