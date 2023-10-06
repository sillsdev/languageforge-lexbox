import type { RequestEvent } from "../$types";
import { redirect } from "@sveltejs/kit";
import { traceRequest } from "$lib/otel/otel.server";

/**
 * A global fallback GET handler for paths that aren't known/mapped by the server
 * GETs from the browser address bar do NOT land here. I'm not quite sure how SvelteKit decides
 * what lands here and what lands in [...catch_all]/+page.server.ts
 * E.g.
 * - /_app/immutable/nodes/1.835f1fca.js
 */
export function GET(event: RequestEvent): Promise<Response> {
  return traceRequest(event, (span) => {
    span.setAttribute('app.request.hit-catch-all', true);
    /**
     * This `if` case would likely lead to an occurence of "error loading dynamically imported module" https://github.com/sillsdev/languageforge-lexbox/issues/210
     * We're not sure how often this happens in staging. We currently only have 1 recorded occurence in honeycomb and it's not the same as the occurence on the linked issue.
     * This server-side trace should be more reliable than the client-side beacon traces
     *
     * We believe the error above is triggered on:
     * - dev: at all sorts of times (due to harmess race conditions caused by HMR etc.)
     * - staging: when a user is active during and after an update/deployment
     * */
    if (event.url.pathname.endsWith('.js')) {
      /**
       * This is kind of cool, but I think we should observe it. I'm slightly wary of this idea, because it could:
       * (1) hide errors and
       * (2) trigger automatic page reloads at undesireable times by a user (e.g.) hovering over a link
       * (Note: we have prompts in place that should prevent users from losing work in forms)
       *  */
      return new Response('location.reload();', {
        headers: {
          ['Content-Type']: 'application/javascript',
        }
      });
    }

    // Default behaviour if this catch-all GET doesn't exist
    throw redirect(307, '/');
  });
}
