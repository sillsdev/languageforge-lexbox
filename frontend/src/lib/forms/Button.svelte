<script lang="ts">
	export let wide: boolean = false;
</script>

<button on:click class='btn variant-filled-primary p-4 text-2xl' class:w-full={wide}>
	<slot />
</button>
