import { is_authn } from '$lib/user'
import { redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types'

export const load = (async ({ cookies }) => {
  if (is_authn(cookies)) {
    throw redirect(307, '/');
  }
}) satisfies PageServerLoad
