import type { LayoutLoadEvent } from './$types';
import { logout, type LexAuthUser } from '$lib/user';

export async function load(event: LayoutLoadEvent) {
  const parentData = await event.parent();
  const user = parentData.user;
  if (!user) {
    logout();
  }

  return { user: user as LexAuthUser };
}
