import { get_authn_token } from '$lib/user'
import { redirect, type Handle } from '@sveltejs/kit'

const public_routes = [
	'/login',
]

export const handle = (async ({ event, resolve }) => {
	const { cookies, locals, url: { pathname } } = event

	if (public_routes.includes(pathname)) {
		return await resolve(event)
	}

	const authn_cookie = get_authn_token(cookies)
	if (! authn_cookie) {
		throw redirect(307, '/login')
	}

	return await resolve(event)
}) satisfies Handle
