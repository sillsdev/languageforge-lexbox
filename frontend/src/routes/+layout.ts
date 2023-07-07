import type { LayoutLoadEvent } from './$types';
import { logout } from '$lib/user';

//setting this to false can help diagnose requests to the api as you can see them in the browser instead of sveltekit
export const ssr = true;
export function load(event: LayoutLoadEvent) {
  const _user = event.data.user;
  if (!_user) {
    logout();
  }

  return { ...event.data, traceParent: event.data.traceParent, userId: _user?.id };
}
