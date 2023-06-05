import type { HandleClientError } from '@sveltejs/kit';
import { getErrorMessage, getTraceId } from './hooks.shared';
import { loadI18n } from '$lib/i18n';
import { trace_error_event } from '$lib/otel/client';

await loadI18n();

export const handleError: HandleClientError = ({ error, event }) => {
  const source = 'client-error-hook';
  // if it's already been traced, we don't want to create another trace just for this error
  // and we want to display the original / real trace-id.
  const traceId = getTraceId(error)
    ?? trace_error_event(error, event, { ['app.error.source']: source });
	const message = getErrorMessage(error);
	return {
		traceId,
		message,
		source,
	};
};
