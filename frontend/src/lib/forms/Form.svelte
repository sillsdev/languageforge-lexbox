<script lang=ts>
	import { createEventDispatcher } from 'svelte'
    import { Turnstile } from 'svelte-turnstile'

	export let protect = false

	const dispatch = createEventDispatcher()
</script>

<!-- https://daisyui.com/components/input/#with-form-control-and-labels -->
<form method=post novalidate on:submit|preventDefault class=form-control>
	<slot />
</form>

{#if protect}
	<section class='mt-8 flex justify-center md:justify-end'>
		<!-- https://github.com/ghostdevv/svelte-turnstile -->
		<Turnstile siteKey={import.meta.env.VITE_TURNSTILE_SITE_KEY} on:turnstile-callback={ ({ detail: { token } }) => dispatch('token', token) } />
	</section>
{/if}

<!-- see frontend/src/app.postcss for global styles related to forms -->
