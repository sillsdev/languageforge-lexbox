import { is_authn } from '$lib/user'
import { redirect, type Handle } from '@sveltejs/kit'
import { initClient } from "$lib/graphQLClient";

const public_routes = [
    '/login',
]

export const handle = (async ({event, resolve}) => {
    const {cookies, locals, url: {pathname}} = event
    initClient(event);
    if (!public_routes.includes(pathname)) {
        if (!is_authn(cookies)) {
            throw redirect(307, '/login')
        }
        locals.user = JSON.parse(await event.cookies.get('user') ?? '');
    }
    return resolve(event, {
        filterSerializedResponseHeaders(name: string, value: string): boolean {
            return true;
        }
    });
}) satisfies Handle
