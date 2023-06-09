import type { RequestHandler } from './$types'
import { logout } from '$lib/user'
import { redirect } from '@sveltejs/kit'

export const GET: RequestHandler = ({ cookies }) => {
  logout(cookies)

  throw redirect(303, '/login')
}
