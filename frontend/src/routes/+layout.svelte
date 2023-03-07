<script>
	import '../theme.postcss';
	import '@skeletonlabs/skeleton/styles/all.css';
	import '../app.postcss';
	import { AppShell } from '@skeletonlabs/skeleton';
	import { AppBar, AppMenu } from '$lib/layout';
</script>

<AppMenu />

<AppShell>
	<svelte:fragment slot="header">
		<AppBar />
	</svelte:fragment>

	<slot />
</AppShell>
