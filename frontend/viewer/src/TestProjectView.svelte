<script lang="ts">
import ProjectView from './ProjectView.svelte';
import {InMemoryDemoApi} from '$lib/in-memory-demo-api';
import ProjectLoader from './ProjectLoader.svelte';

const projectName = InMemoryDemoApi.setup().projectName;
let loading = $state(true);
</script>

<ProjectLoader {projectName} {loading}>
  <ProjectView onloaded={() => loading = false}></ProjectView>
</ProjectLoader>
