import { logout } from '$lib/user'
import type { PageServerLoad } from './$types'

export const load = (async ({ cookies }) => {
	logout(cookies)

	return;
}) satisfies PageServerLoad
