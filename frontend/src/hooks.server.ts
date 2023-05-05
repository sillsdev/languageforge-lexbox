import { is_authn } from '$lib/user'
import { redirect, type Handle, type HandleFetch, type HandleServerError, type ResolveOptions } from '@sveltejs/kit'
import { initClient } from '$lib/graphQLClient'
import {loadI18n} from "$lib/i18n";
import { traceErrorEvent, traceResponse, traceRequest } from '$lib/otel/server'
import {env} from "$env/dynamic/private";

const public_routes = [
	'/login',
	'/register',
	'/email',
	'/forgotPassword',
	'/forgotPassword/emailSent',
]

export const handle = (async ({ event, resolve }) => {
	return traceRequest(event, async () => {
		await loadI18n();
		const { cookies, url: { pathname } } = event
		const options: ResolveOptions = {
			filterSerializedResponseHeaders: () => true,
		}

		initClient(event, env.BACKEND_HOST)

		return traceResponse(event, () => {
			if (public_routes.includes(pathname)) {
				return resolve(event, options)
			}

			if (!is_authn(cookies)) {
				throw redirect(307, '/login')
			}

			return resolve(event, options)
		})
	})
}) satisfies Handle
export const handleFetch = (async ({event, request, fetch}) => {
	if (env.BACKEND_HOST && request.url.startsWith(env.BACKEND_HOST)) {
		request.headers.set('cookie', event.request.headers.get('cookie')!);
	}

	return fetch(request);
}) satisfies HandleFetch;

export const handleError: HandleServerError = ({ error, event }) => {
	traceErrorEvent(error, event);
	console.error(error);
}
