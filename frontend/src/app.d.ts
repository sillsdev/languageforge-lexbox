/// <reference types='@sveltejs/kit' />

import type { LexAuthUser } from '$lib/user';
import type {Client} from '@urql/svelte';

export { }; // for some reason this is required in order to make global changes

declare global {
  interface Window {
    lexbox: { fetchProxy: Fetch }
  }

  type LexboxResponseHandlingConfig = {
    disableRedirectOnAuthError?: true;
  }

  interface RequestInit {
    lexboxResponseHandlingConfig?: LexboxResponseHandlingConfig;
  }

  namespace App {
    interface Locals {
      client: Client;
      getUser: (() => LexAuthUser | null);
      requestedLocale?: string;
    }

    interface Error {
      traceId: string;
      handler: ErrorHandler;
      updateDetected?: boolean;
    }
  }

  type Fetch = typeof window.fetch;

  type ErrorHandler =
    'client-error' | 'client-unhandledrejection' |
    'server-error-hook' | 'client-error-hook';

  function enableDevMode(): void;
}
