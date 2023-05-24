<script lang="ts">
	export let icon: string;
    export let disabled = false;
</script>

<button {disabled} on:click class="btn btn-square btn-outline">
	<span class="{icon} text-lg" />
</button>
