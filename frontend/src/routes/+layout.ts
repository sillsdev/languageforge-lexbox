import type {LayoutLoadEvent} from './$types'
import {initClient} from "$lib/graphQLClient";
import { logout, user } from '$lib/user'
import {browser} from "$app/environment";
import { get } from 'svelte/store';

export function load(event: LayoutLoadEvent) {
  const _user = event.data.user ?? get(user);
  if (!_user) {
    logout();
  }
	user.set(_user);
	if (browser) initClient(event);
	return { traceParent: event.data.traceParent, userId: _user?.id };
}
