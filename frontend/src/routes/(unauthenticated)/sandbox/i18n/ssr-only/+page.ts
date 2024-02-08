import type { PageLoadEvent } from './$types';
import { delay } from '$lib/util/time';

export const csr = false;

export async function load(event: PageLoadEvent) {
  const loadDelay = Number(event.url.searchParams.get('delay'));
  if (!isNaN(loadDelay) && loadDelay > 0) {
    await delay(loadDelay);
  }
}
