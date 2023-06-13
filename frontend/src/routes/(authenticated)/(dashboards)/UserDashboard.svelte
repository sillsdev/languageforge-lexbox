<script lang="ts">
  import ProjectList from '$lib/components/ProjectList.svelte';
  import t from '$lib/i18n';
  import { Page } from '$lib/layout';
  import type { Project } from '$lib/project';

  export let projects: Project[] = [];
</script>

<Page>
  <svelte:fragment slot="header">
    {$t('user_dashboard.page_header')}
  </svelte:fragment>

  <ProjectList {projects} />
</Page>
