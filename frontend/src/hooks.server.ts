import { redirect, type Handle } from '@sveltejs/kit';

const public_routes = [
	'/login',
]

export const handle = (async ({ event, resolve }) => {
	const { cookies, locals, url: { pathname } } = event

	if (public_routes.includes(pathname)) {
		return await resolve(event)
	}

	const user_cookie = cookies.get('user')
	if (! user_cookie) {
		throw redirect(307, '/login')
	}

	locals.user = JSON.parse(await event.cookies.get('user') ?? '')

	return await resolve(event)
}) satisfies Handle;
