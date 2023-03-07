<script lang="ts">
    import { page } from '$app/stores';
	import { Page } from '$lib/layout';
</script>

<Page>
	<svelte:fragment slot="header">
		{$page.status}: {$page.error?.message}
	</svelte:fragment>
</Page>
