import type { LayoutLoadEvent } from './$types';

//setting this to false can help diagnose requests to the api as you can see them in the browser instead of sveltekit
export const ssr = true;
export function load(event: LayoutLoadEvent) {
  return { ...event.data, traceParent: event.data.traceParent };
}
