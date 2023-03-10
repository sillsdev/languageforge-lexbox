import { get_authn_token, parse_authn_token, user } from '$lib/user';
import { get } from 'svelte/store';
import type { LayoutServerLoadEvent } from '../../.svelte-kit/types/src/routes/$types';

export function load(event: LayoutServerLoadEvent) {
	let authnToken = get_authn_token(event.cookies);
	return {
		user: authnToken ? parse_authn_token(authnToken) : null,
	};
}
