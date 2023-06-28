import { ensureErrorIsTraced, traceFetch } from '$lib/otel/otel.client';

import { redirect, type HandleClientError } from '@sveltejs/kit';
import { getErrorMessage } from './hooks.shared';
import { loadI18n } from '$lib/i18n';
import { handleFetch } from '$lib/util/fetch-proxy';

await loadI18n();

export const handleError: HandleClientError = ({ error, event }) => {
  const handler = 'client-error-hook';
  const traceId = ensureErrorIsTraced(error, { event }, { ['app.error.source']: handler });
  const message = getErrorMessage(error);
  return {
    traceId,
    message,
    handler,
  };
};

/**
 * This is obviously NOT a SvelteKit handler/feature. It just mimics the `handleFetch` in hooks.server.ts.
 */
handleFetch(async ({ fetch, args }) => {
  const response = await traceFetch(() => fetch(...args));
  if (response.status === 401) {
    throw redirect(307, '/logout');
  }
  return response;
});
