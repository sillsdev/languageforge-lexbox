import { is_authn } from '$lib/user'
import { redirect, type Handle, type HandleServerError, type ResolveOptions } from '@sveltejs/kit'
import { initClient } from '$lib/graphQLClient'
import {loadI18n} from "$lib/i18n";
import { traceErrorEvent, traceResponse, traceRequest } from '$lib/otel/server'

const public_routes = [
	'/login',
]

export const handle = (async ({ event, resolve }) => {
	return traceRequest(event, async () => {
		await loadI18n();
		const { cookies, url: { pathname } } = event
		const options: ResolveOptions = {
			filterSerializedResponseHeaders: () => true,
		}

		initClient(event)

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

export const handleError: HandleServerError = ({ error, event }) => {
	traceErrorEvent(error, event)
}
