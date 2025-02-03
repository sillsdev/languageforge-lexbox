<script lang="ts">
import ProjectView from './ProjectView.svelte';
import {InMemoryApiService} from '$lib/in-memory-api-service';
import ProjectLoader from './ProjectLoader.svelte';

const projectName = InMemoryApiService.setup().projectName;
</script>

<ProjectLoader {projectName} let:onProjectLoaded>
  <ProjectView {projectName} isConnected on:loaded={e => onProjectLoaded(e.detail)}></ProjectView>
</ProjectLoader>
