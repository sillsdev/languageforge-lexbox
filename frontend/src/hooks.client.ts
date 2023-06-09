import type { HandleClientError } from '@sveltejs/kit';
import { getErrorMessage } from './hooks.shared';
import { loadI18n } from '$lib/i18n';
import { traceErrorEvent } from '$lib/otel/client';

await loadI18n();

export const handleError: HandleClientError = ({ error, event }) => {
  const source = 'client-error-hook';
  const traceId = traceErrorEvent(error, event, { ['app.error.source']: source });
  const message = getErrorMessage(error);
  return {
    traceId,
    message,
    source,
  };
};
