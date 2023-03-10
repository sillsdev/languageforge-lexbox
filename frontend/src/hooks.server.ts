import { is_authn } from '$lib/user'
import { redirect, type Handle } from '@sveltejs/kit'

const public_routes = [
	'/login',
]

export const handle = (async ({ event, resolve }) => {
	const { cookies, url: { pathname } } = event

	if (public_routes.includes(pathname)) {
		return await resolve(event)
	}

	if (! is_authn(cookies)) {
		throw redirect(307, '/login')
	}

	return await resolve(event)
}) satisfies Handle
