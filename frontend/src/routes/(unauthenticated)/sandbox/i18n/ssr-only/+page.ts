import type { PageLoadEvent } from './$types';

export const csr = false;

export async function load(event: PageLoadEvent) {
  const delay = Number(event.url.searchParams.get('delay'));
  if (!isNaN(delay) && delay > 0) {
    await new Promise(resolve => setTimeout(resolve, delay));
  }
}
