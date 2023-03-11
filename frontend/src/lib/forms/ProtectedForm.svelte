<script context=module lang=ts>
	export type Token = {
		token: string,
	}
</script>

<script lang=ts>
	import { createEventDispatcher } from 'svelte'
    import { Turnstile } from 'svelte-turnstile'
	import Form from './Form.svelte'

	const dispatch = createEventDispatcher<Token>()
	const siteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY as string

	function deliver_token({ detail: { token } }: CustomEvent<Token>) {
		dispatch('token', token)
	}
</script>

<Form on:submit>
	<slot />
</Form>

<section class='mt-8 flex justify-center md:justify-end'>
	<!-- https://github.com/ghostdevv/svelte-turnstile -->
	<Turnstile {siteKey} on:turnstile-callback={deliver_token} />
</section>
