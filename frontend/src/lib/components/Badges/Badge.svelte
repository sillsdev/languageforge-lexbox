<script lang="ts">
	import { Badge } from '.';

	export let type = '';
	export let button = false;
	export let icon = '';
</script>

{#if button}
	<button on:click class="hover:drop-shadow-xl hover:brightness-90">
		<Badge {type} {icon}>
			<slot />
		</Badge>
	</button>
{:else}
	<span class="badge badge-lg {type} p-4">
		<slot />

		{#if icon}
			<span class="{icon} text-lg ml-2" />
		{/if}
	</span>
{/if}
