<script lang="ts">
  import type { PageData } from './$types';
  import t from "$lib/i18n";
  import ProjectList from "$lib/components/ProjectList.svelte";
  import {Page} from "$lib/layout";

  export let data: PageData;
  $: projects = data.projects;
</script>

<Page>
  <svelte:fragment slot="header">
    {$t('user_dashboard.title')}
  </svelte:fragment>

  <ProjectList projects={$projects}/>
</Page>
