import { logout, user } from '$lib/user';

import type { LayoutLoadEvent } from './$types';
import { browser } from '$app/environment';
import { get } from 'svelte/store';
import { initClient } from '$lib/gql';

export function load(event: LayoutLoadEvent) {
  const _user = event.data.user ?? get(user);
  if (!_user) {
    logout();
  }
  user.set(_user);
  if (browser) initClient();
  return { traceParent: event.data.traceParent, userId: _user?.id };
}
