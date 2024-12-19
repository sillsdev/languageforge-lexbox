<script lang="ts">
  import ProjectView from './ProjectView.svelte';

  export let projectName: string;
</script>
<ProjectView {projectName} isConnected></ProjectView>
