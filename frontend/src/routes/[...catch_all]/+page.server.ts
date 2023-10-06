import { error } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';
import { traceRequest } from '$lib/otel/otel.server';

/**
 * A global fallback GET handler for routes that aren't known/mapped by the server
 * E.g.
 * - /kevin
 * - /admin/kevin
 */
export const load = ((event) => {
  return traceRequest(event, (span) => {
    span.setAttribute('app.request.hit-catch-all-page', true);
    throw error(404);
  });
}) satisfies PageServerLoad
