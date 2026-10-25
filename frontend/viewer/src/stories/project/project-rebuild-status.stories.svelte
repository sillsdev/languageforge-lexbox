<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import ProjectRebuildStatus from '$project/ProjectRebuildStatus.svelte';

  const {Story} = defineMeta({
    component: ProjectRebuildStatus,
    args: {projectName: 'Sena 3'},
  });
</script>

<Story name="Rebuilding" />
