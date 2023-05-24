<script lang="ts">
	import { TaggedBadge } from '.';
    
	import Badge from './Badge.svelte';
	export let button = false;
	export let icon = '';
</script>

{#if button}
	<button on:click class="hover:drop-shadow-xl hover:brightness-90">
		<TaggedBadge {icon}>
			<slot />
			<slot slot="tag" name="tag" />
		</TaggedBadge>
	</button>
{:else}
	<span class="parent-badge badge badge-lg badge-primary p-4 pr-0">
		<span class="pr-2">
			<slot />
		</span>

		<Badge>
			<slot name="tag" />
		</Badge>

		{#if icon}
			<span class="{icon} text-lg ml-1 mr-3" />
		{/if}
	</span>
{/if}

<style>
	.parent-badge {
		border-right: none;
	}
</style>
