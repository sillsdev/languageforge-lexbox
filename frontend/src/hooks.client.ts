import { loadI18n } from '$lib/i18n';
import { trace_error_event } from '$lib/otel/client';
import type { HandleClientError } from '@sveltejs/kit';

await loadI18n();

export const handleError: HandleClientError = ({ error, event }) => {
  trace_error_event(error, event)
}
