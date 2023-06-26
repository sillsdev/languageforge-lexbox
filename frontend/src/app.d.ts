/// <reference types='@sveltejs/kit' />

import type { LexAuthUser } from '$lib/user';

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
      getUser: (() => LexAuthUser | null);
    }

    interface Error {
      traceId: string;
      handler: ErrorHandler;
    }
  }

  type ErrorHandler =
    'client-error' | 'client-unhandledrejection' |
    'server-error-hook' | 'client-error-hook';
}
