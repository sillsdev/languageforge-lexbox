import type { PageLoadEvent } from '../$types';
import { redirect } from '@sveltejs/kit';

export async function load(event: PageLoadEvent) {
  const data = await event.parent();
  throw redirect(307, data.home);
}
