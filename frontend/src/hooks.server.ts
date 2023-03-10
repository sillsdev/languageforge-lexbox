import { get_authn_token } from '$lib/user'
import { redirect, type Handle } from '@sveltejs/kit'
import {client, getClient} from "$lib/graphQLClient";
import {createClient} from "@urql/svelte";

const public_routes = [
	'/login',
]

export const handle = (async ({ event, resolve }) => {
	const { cookies, locals, url: { pathname } } = event
	if (!public_routes.includes(pathname)) {
		const authn_cookie = get_authn_token(cookies)
		if (!authn_cookie) {
			throw redirect(307, '/login')
		}
		locals.user = JSON.parse(await event.cookies.get('user') ?? '');
	}
	client.set(createClient({
		url: "/api/graphql",
		fetch: event.fetch,
	}));
	return resolve(event, {
		filterSerializedResponseHeaders(name: string, value: string): boolean {
			return true;
		}
	});
}) satisfies Handle
