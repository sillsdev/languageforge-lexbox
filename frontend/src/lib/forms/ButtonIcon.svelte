<script lang="ts">
	import Icon from '$lib/Icon.svelte';

	export let name: string
</script>

<!-- had to override some of skeleton's btn-icon styles to get icon to show properly -->
<button on:click class='btn-icon p-0 w-auto'>
	<Icon {name} />
</button>
