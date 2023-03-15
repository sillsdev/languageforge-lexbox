<script lang="ts">
	export let policy: number;
</script>

{#if policy === 1}
	Verified
{:else if policy === 2}
	Test
{:else if policy === 3}
	Dev
{:else}
	Unknown
{/if}
