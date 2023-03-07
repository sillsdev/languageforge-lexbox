<script lang="ts">
	import Icon from '@iconify/svelte';
	import Tooltip from '$lib/Tooltip.svelte';

	export let name: string;
	export let tooltip: string = ''
</script>

{#if tooltip}
	<Tooltip text={tooltip}>
		<Icon icon={name} />
	</Tooltip>
{:else}
	<Icon icon={name} />
{/if}

<!-- see frontend/src/app.postcss for global styles related to Icon -->
