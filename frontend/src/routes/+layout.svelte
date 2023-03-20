<script lang=ts>
	import '$lib/app.postcss'
	import { AppBar, AppMenu } from '$lib/layout'
	import { get_trace_parent } from '$lib/otel';
	import { user } from '$lib/user'

	let menu_toggle = false

	function open() {
		menu_toggle = true
	}

	function close() {
		menu_toggle = false
	}

	function closeOnEscape(event: KeyboardEvent) {
		event.key === 'Escape' && close()
	}

	// https://www.w3.org/TR/trace-context/#traceparent-header
	// so the page-load instrumentation can be correlated with the server load
	const traceParent = get_trace_parent()
</script>

<svelte:window on:keydown={ closeOnEscape } />

<svelte:head>
	{#if traceParent}
		<meta name="traceparent" content={traceParent} />
	{/if}
</svelte:head>

<!-- https://daisyui.com/components/drawer -->
<div class='drawer drawer-end'>
	<input type=checkbox checked={ menu_toggle } class=drawer-toggle>

	<div class=drawer-content>
		<AppBar on:menuopen={ open } />

		<!-- https://tailwindcss.com/docs/typography-plugin -->
		<main class='max-w-none px-6'>
			<slot />
		</main>
	</div>

	{#if $user}
		<AppMenu on:click={ close } on:keydown={ close } />
	{/if}
</div>
