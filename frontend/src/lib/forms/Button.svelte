<script lang="ts">
	export let loading = false;
</script>

<!-- https://daisyui.com/components/button -->
<button on:click class="btn btn-primary" class:loading>
	<slot />
</button>
