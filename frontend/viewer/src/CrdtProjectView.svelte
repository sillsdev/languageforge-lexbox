<script lang="ts">
  import ProjectLoader from './ProjectLoader.svelte';
  import ProjectView from './ProjectView.svelte';

  export let projectName: string;
</script>

<ProjectLoader {projectName} let:onProjectLoaded>
  <ProjectView isConnected onloaded={onProjectLoaded}></ProjectView>
</ProjectLoader>
