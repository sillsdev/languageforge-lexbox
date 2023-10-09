import type { RequestEvent } from '../$types';
import { traceRequest } from '$lib/otel/otel.server';

/**
 * A global fallback GET handler for paths that aren't known/mapped by the server
 * E.g. /_app/immutable/nodes/1.835f1fca.js
 * GETs from the browser address bar WOULD land here if we didn't have the global fallback `load()` in [...catch_all]/+page.server.ts.
 */
export function GET(event: RequestEvent): Promise<Response> {
  /**
   * If a JS file is being requested this will potentially lead to an occurence of "error loading dynamically imported module" https://github.com/sillsdev/languageforge-lexbox/issues/210
   * although SvelteKit is pretty good at recovering from that state.
   *
   * We're not sure why or how often this happens in staging. We currently only have 1 recorded occurence in honeycomb and it's not the same as the occurence on the linked issue.
   * It potentially happens when a user is active during and after an update/deployment (though SK seems to recover in that case)
   * It seems likely that this is triggered on dev due to harmless race conditions caused by compilation/HMR etc.
   *
   * This server-side trace attribute should be valuable, because it should be more reliable than the client-side beacon traces.
   * */
  return traceRequest(event, (span) => {
    span.setAttribute('app.request.hit-catch-all', true);
    return new Response(null, { status: 404 });
    // Default behaviour if this catch-all GET didn't exist. A 404 seems to make a lot more sense:
    // throw redirect(307, '/');
  });
}
