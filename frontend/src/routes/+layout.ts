import type { LayoutLoadEvent } from './$types';
import { browser } from '$app/environment';
import { loadI18n } from '$lib/i18n';

//setting this to false can help diagnose requests to the api as you can see them in the browser instead of sveltekit
export const ssr = true;

export async function load(event: LayoutLoadEvent) {
  if (browser) { // done server-side in hooks.server.ts
    await loadI18n(event.data.activeLocale);
  }
  return event.data;
}
