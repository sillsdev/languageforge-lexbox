import type {LayoutLoadEvent} from './$types'
import {initClient} from "$lib/graphQLClient";
import { user } from '$lib/user'
import {browser} from "$app/environment";

export function load(event: LayoutLoadEvent)  {
    if (browser) initClient(event);
    user.set(event.data.user);
    return { traceParent: event.data.traceParent };
}
