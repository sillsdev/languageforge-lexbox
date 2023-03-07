import { user } from '$lib/user';
import type { LayoutLoad } from './$types';

export const load = (async ({ data }) => {
	user.set(data.user)
}) satisfies LayoutLoad;
