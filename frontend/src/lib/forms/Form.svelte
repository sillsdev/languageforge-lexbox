<script lang=ts>
	import { createEventDispatcher } from 'svelte';
    import { Turnstile } from 'svelte-turnstile'

	export let protect = false

	const dispatch = createEventDispatcher()
</script>

<!-- https://daisyui.com/components/input/#with-form-control-and-labels -->
<form method=post novalidate on:submit|preventDefault class=form-control>
	<slot />

	{#if protect}
		<Turnstile siteKey=1x00000000000000000000AA on:turnstile-callback={ ({ detail: { token } }) => dispatch('token', token) } />
	{/if}
</form>

<!-- see frontend/src/app.postcss for global styles related to forms -->
