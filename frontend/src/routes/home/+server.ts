import type { RequestHandler } from './$types'
import { isAdmin, logout } from '$lib/user'
import { redirect } from '@sveltejs/kit'

export const GET: RequestHandler = ({ locals }) => {
  const user = locals.getUser();
  if (user) {
    const dest = isAdmin(user) ? '/admin' : '/';
    throw redirect(307, dest);
  } else {
    throw redirect(303, '/login');
  }
}
