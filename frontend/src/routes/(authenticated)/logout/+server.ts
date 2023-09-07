import type {RequestEvent} from './$types'
import { logout } from '$lib/user'
import { redirect } from '@sveltejs/kit'

export function GET({cookies}: RequestEvent) : void {
  logout(cookies)

  throw redirect(303, '/login')
}
