import { logout } from '$lib/user'
import { redirect } from '@sveltejs/kit'
import type { RequestHandler } from './$types'

export const GET: RequestHandler = async ({cookies}) => {
	logout(cookies)

	throw redirect(303, '/login')
}
