/// <reference types='@sveltejs/kit' />

import type { LexAuthUser } from '$lib/user';

export { }; // for some reason this is required in order to make global changes

declare global {
  interface Window {
    lexbox: { fetchProxy: Fetch }
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

  type Fetch = typeof window.fetch;

  type ErrorHandler =
    'client-error' | 'client-unhandledrejection' |
    'server-error-hook' | 'client-error-hook';
}
