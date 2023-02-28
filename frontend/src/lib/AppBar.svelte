<script>
	import { AppBar } from '@skeletonlabs/skeleton';

</script>

<AppBar background="bg-primary-500 text-on-primary-token">
	<svelte:fragment slot="lead">
		<strong class="text-lg">Language Depot</strong>
	</svelte:fragment>

	<svelte:fragment slot="trail">
		guest
	</svelte:fragment>
</AppBar>
