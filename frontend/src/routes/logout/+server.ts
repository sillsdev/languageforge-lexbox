import { redirect } from '@sveltejs/kit'
import type { RequestHandler } from './$types'

export const GET: RequestHandler = async ({cookies}) => {
	cookies.delete('user')

	throw redirect(303, '/login')
}
