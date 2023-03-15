<script lang="ts">
	export let type: number;
</script>

{#if type === 1}
	FLEx
{:else}
	Unknown
{/if}
