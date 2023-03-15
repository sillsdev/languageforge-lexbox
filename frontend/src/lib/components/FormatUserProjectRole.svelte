<script lang="ts">
	export let projectRole: number;
</script>

{#if projectRole === 2}
	Manager
{:else if projectRole === 3}
	Editor
{:else}
	Unknown
{/if}
