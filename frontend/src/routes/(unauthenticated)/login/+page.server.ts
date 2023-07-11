import { isAuthn } from '$lib/user';
import { redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

export const load = (({ cookies }) => {
  if (isAuthn(cookies)) {
    throw redirect(307, '/home');
  }
}) satisfies PageServerLoad
