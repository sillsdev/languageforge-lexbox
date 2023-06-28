import type { LayoutLoadEvent } from './$types';
import { logout } from '$lib/user';

export function load(event: LayoutLoadEvent) {
  const _user = event.data.user;
  if (!_user) {
    logout();
  }

  return { ...event.data, traceParent: event.data.traceParent, userId: _user?.id };
}
