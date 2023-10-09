import { ensureErrorIsTraced, traceFetch } from '$lib/otel/otel.client';

import { redirect, type HandleClientError } from '@sveltejs/kit';
import { getErrorMessage } from './hooks.shared';
import { loadI18n } from '$lib/i18n';
import { handleFetch } from '$lib/util/fetch-proxy';
import {invalidate} from '$app/navigation';
import {USER_LOAD_KEY} from '$lib/user';
import { updated } from '$app/stores';

await loadI18n();

// eslint-disable-next-line func-style
export const handleError: HandleClientError = async ({ error, event }) => {
  const handler = 'client-error-hook';

  // someone has to have subscribed to updated before `check` is allowed to be called
  // this seems to work and provide the correct value :shrug:
  // eslint-disable-next-line @typescript-eslint/no-empty-function
  updated.subscribe(() => { })();
  const updateDetected = await updated.check();

  const traceId = ensureErrorIsTraced(error, { event }, {
    ['app.error.source']: handler,
    ...(updateDetected ? { ['app.update-detected']: true } : {}),
  });
  const message = getErrorMessage(error);
  return {
    traceId,
    message,
    handler,
    updateDetected,
  };
};

/**
 * This is obviously NOT a SvelteKit handler/feature. It just mimics the `handleFetch` in hooks.server.ts.
 */
handleFetch(async ({ fetch, args }) => {
  const response = await traceFetch(() => fetch(...args));
  if (response.status === 401 && location.pathname !== '/login') {
    throw redirect(307, '/logout');
  }
  if (response.headers.get('lexbox-refresh-jwt') == 'true') {
    await invalidate(USER_LOAD_KEY);
  }
  return response;
});
