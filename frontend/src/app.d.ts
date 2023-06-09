/// <reference types='@sveltejs/kit' />

export { }; // for some reason this is required in order to make global changes

declare global {
  interface Window {
    /* eslint-disable @typescript-eslint/naming-convention */
    fetch_original: typeof window.fetch;
    fetch_otel_instrumented: typeof window.fetch;
    /* eslint-enable @typescript-eslint/naming-convention */
  }

  namespace App {
    interface Locals {
      client: import('@urql/svelte').Client;
    }

    interface Error {
      traceId: string;
      source: ErrorSource;
    }
  }

  type ErrorSource = 'client-error' | 'client-unhandledrejection' | 'server-error-hook' | 'client-error-hook';
}
