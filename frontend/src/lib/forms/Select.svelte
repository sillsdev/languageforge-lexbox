<script lang="ts">
	export let label: string;
	export let id: string;
	export let autofocus: boolean = false;
	export let error: string | string[] = '';
	export let value: any;
</script>

<label for={id} class="label">
	<span class="label-text">
		{label}
	</span>
</label>
<select {autofocus} bind:value {id} class="select select-bordered">
	<slot />
</select>

{#if error}
	<label for={id} class="label">
		<span class="label-text-alt text-error mb-2"
			>{typeof error === 'string' ? error : error.join(', ')}</span
		>
	</label>
{/if}
