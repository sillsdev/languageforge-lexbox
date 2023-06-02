import type {LayoutLoadEvent} from './$types'
import {initClient} from "$lib/graphQLClient";
import { user } from '$lib/user'
import {browser} from "$app/environment";

export function load(event: LayoutLoadEvent)  {
  if (browser) initClient(event);
  const _user = event.data.user;
  user.set(_user);
  return { traceParent: event.data.traceParent, userId: _user?.id };
}
