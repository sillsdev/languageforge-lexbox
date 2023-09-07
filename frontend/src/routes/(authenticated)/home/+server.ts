import type {RequestEvent} from './$types'
import { getHomePath } from '$lib/user'
import { redirect } from '@sveltejs/kit'

export function GET({locals}: RequestEvent) : void {
  const user = locals.getUser();
  if (user) {
    const dest = getHomePath(user);
    throw redirect(307, dest);
  } else {
    throw redirect(303, '/login');
  }
}
