<script>
	import '../theme.postcss';
	import '@skeletonlabs/skeleton/styles/all.css';
	import '../app.postcss';
	import { AppShell } from '@skeletonlabs/skeleton';
	import AppBar from '$lib/AppBar.svelte';
</script>

<AppShell>
	<svelte:fragment slot="header">
		<AppBar />
	</svelte:fragment>

	<slot />
</AppShell>
