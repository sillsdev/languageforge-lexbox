import type { LayoutLoadEvent } from './$types';
import { loadI18n } from '$lib/i18n';

//setting this to false can help diagnose requests to the api as you can see them in the browser instead of sveltekit
export const ssr = true;

export async function load(event: LayoutLoadEvent) {
  await loadI18n(event.data.user?.locale);
  return event.data;
}
