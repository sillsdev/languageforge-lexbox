import { logout } from '$lib/user';

import type { LayoutLoadEvent } from './$types';
import { browser } from '$app/environment';
import { initClient } from '$lib/gql';

export async function load(event: LayoutLoadEvent) {
  const _user = event.data.user;
  if (!_user) {
    logout();
  }
  if (browser) initClient();
  return { ...event.data, traceParent: event.data.traceParent, userId: _user?.id };
}
