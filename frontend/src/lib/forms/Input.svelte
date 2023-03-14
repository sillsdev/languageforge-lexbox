<script lang=ts>
	import { onMount } from 'svelte'

	export let label: string
	export let value: string = ''
	export let type: string = 'text'
	export let required: boolean = false
	export let autofocus: boolean = false
	export let error: string = ''
	export let placeholder: string = '';

	let id = crypto.randomUUID().split('-').at(-1)
	let input: HTMLInputElement

	onMount(autofocus_if_requested)

	function autofocus_if_requested() {
		autofocus && input?.focus()
	}

	// works around "svelte(invalid-type)" warning, i.e., can't have a dynamic type AND bind:value...keep an eye on https://github.com/sveltejs/svelte/issues/3921
	function type_workaround(node: HTMLInputElement) {
		node.type = type
	}
</script>

<!-- https://daisyui.com/components/input -->
<label for={ id } class=label>
	<span class=label-text>
		{ label }
	</span>
</label>

<input { id } use:type_workaround bind:value { required } class:input-error={error} { placeholder } bind:this={ input } class='input input-bordered' />

{#if error}
	<label for={ id } class=label>
		<span class='label-text-alt text-error mb-2'>{error}</span>
	</label>
{/if}
