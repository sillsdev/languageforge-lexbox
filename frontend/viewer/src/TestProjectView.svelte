<script lang="ts">
import ProjectView from './ProjectView.svelte';
import {InMemoryApiService} from '$lib/in-memory-api-service';
import ProjectLoader from './ProjectLoader.svelte';

const projectName = InMemoryApiService.setup().projectName;
let loading = $state(true);
</script>

<ProjectLoader {projectName} {loading}>
  <ProjectView onloaded={() => loading = false}></ProjectView>
</ProjectLoader>
