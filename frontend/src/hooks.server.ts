import { is_authn } from '$lib/user'
import { redirect, type Handle, type ResolveOptions } from '@sveltejs/kit'
import { initClient } from "$lib/graphQLClient"

const public_routes = [
	'/login',
]

export const handle = (({ event, resolve }) => {
	const { cookies, url: { pathname } } = event
	const options: ResolveOptions = {
		filterSerializedResponseHeaders: () => true
	}

	initClient(event)

	if (public_routes.includes(pathname)) {
		return resolve(event, options)
	}

	if (!is_authn(cookies)) {
		throw redirect(307, '/login')
	}

	return resolve(event, options)
}) satisfies Handle
