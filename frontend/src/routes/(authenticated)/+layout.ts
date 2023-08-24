import type { LayoutLoadEvent } from './$types';
import { logout, type LexAuthUser, getHomePath } from '$lib/user';

export async function load(event: LayoutLoadEvent) {
  const parentData = await event.parent();
  const user = parentData.user;
  if (!user) {
    logout();
  }

  // Storing the actual home path means we don't need to reload the document
  // (by explicitly navigating to /home)
  const home = getHomePath(user);

  return { user: user as LexAuthUser, home };
}
