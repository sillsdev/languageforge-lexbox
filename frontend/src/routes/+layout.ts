import { user } from '$lib/user'
import type {LayoutLoadEvent} from './$types'
import {initClient} from "$lib/graphQLClient";

export function load(event: LayoutLoadEvent)  {
    initClient(event);
    user.set(event.data.user);
}
