import { get_user } from '$lib/user'
import type { LayoutServerLoadEvent } from './$types'

export function load({ cookies }: LayoutServerLoadEvent) {
	const user = get_user(cookies)

	return {
		user,
	}
}
