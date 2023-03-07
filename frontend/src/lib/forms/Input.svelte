<script lang="ts">
	import { onMount } from 'svelte';

	export let label: string;
	export let name: string;
	export let value: string = '';
	export let type: string = 'text';
	export let required: boolean = false;
	export let autofocus: boolean = false;
	export let error: string = '';

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

<label class="label">
	<span>{label}</span>

	<input {name} bind:value class="input p-4" class:input-error={error} bind:this={input} use:type_workaround {required}>
	{#if error}
		<span class="text-sm text-error-500 ml-4">{error}</span>
	{/if}
</label>
