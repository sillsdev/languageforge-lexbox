<script lang="ts">
	export let icon: string;
    export let disabled = false;
    export let loading = false;
</script>

<button {disabled} on:click class="btn btn-square btn-outline" class:loading={loading}>
    {#if !loading}
	    <span class="{icon} text-lg" />
    {/if}
</button>
