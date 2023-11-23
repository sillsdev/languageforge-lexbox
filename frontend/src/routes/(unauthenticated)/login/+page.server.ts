import { goHome, isAuthn } from '$lib/user';
import type { PageServerLoad } from './$types';

export const load = (async ({ cookies }) => {
  if (isAuthn(cookies)) {
    await goHome();
  }
}) satisfies PageServerLoad
