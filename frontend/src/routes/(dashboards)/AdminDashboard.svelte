<script lang="ts">
	import {Page} from '$lib/layout'
	import ProjectList from "$lib/components/ProjectList.svelte";

	export let projects = [];
</script>

<Page>
	<svelte:fragment slot=header>
		Projects
	</svelte:fragment>
	<ProjectList projects={projects}/>
</Page>
