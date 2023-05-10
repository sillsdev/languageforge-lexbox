<script context=module lang=ts>
	export type Token = {
		token: string,
	};
</script>

<script lang=ts>
	import { createEventDispatcher } from 'svelte'
    import { Turnstile } from 'svelte-turnstile'
	import Form from './Form.svelte'
	import {env} from "$env/dynamic/public";
	import type { SuperForm } from "sveltekit-superforms/client";


	export let enhance: SuperForm<any>["enhance"] | undefined = undefined;

	const siteKey = env.PUBLIC_TURNSTILE_SITE_KEY;
	export let turnstileToken: string = '';

	function deliver_token({ detail: { token } }: CustomEvent<Token>) {
		turnstileToken = token;
	}
</script>

<Form {enhance} on:submit>
	<slot />
</Form>

<section class='mt-8 flex justify-center md:justify-end'>
	<!-- https://github.com/ghostdevv/svelte-turnstile -->
	<Turnstile {siteKey} on:turnstile-callback={deliver_token} />
</section>
