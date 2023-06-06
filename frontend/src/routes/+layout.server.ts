import type { LayoutServerLoadEvent } from './$types'
import { getRootTraceparent } from '$lib/otel/server'
import { get_user } from '$lib/user'

export function load({ cookies }: LayoutServerLoadEvent) {
	const user = get_user(cookies)
	const traceParent = getRootTraceparent()

	return {
		user,
		traceParent,
	}
}
