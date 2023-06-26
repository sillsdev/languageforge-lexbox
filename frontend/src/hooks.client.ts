import type { HandleClientError } from '@sveltejs/kit';
import { ensureErrorIsTraced } from '$lib/otel/client';
import { getErrorMessage } from './hooks.shared';
import { loadI18n } from '$lib/i18n';

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
