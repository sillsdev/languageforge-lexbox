import type {LayoutLoadEvent} from './$types'
import {initClient} from "$lib/graphQLClient";
import { user } from '$lib/user'

export function load(event: LayoutLoadEvent)  {
    initClient(event);
    user.set(event.data.user);
    return { traceParent: event.data.traceParent };
}
