<script lang="ts">
  import type { PageData } from './$types';
  import t from '$lib/i18n';
  import ProjectList from '$lib/components/ProjectList.svelte';
  import { HeaderPage } from '$lib/layout';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import type { ProjectType } from '$lib/gql/types';
  import { ProjectFilter, type ProjectFilters, type ProjectItem } from '$lib/components/Projects';
  import ProjectTable from '$lib/components/Projects/ProjectTable.svelte';
  import { Button } from '$lib/forms';
  import { browser } from '$app/environment';
  import { limit } from '$lib/components/Paging';
  import IconButton from '$lib/components/IconButton.svelte';

  export let data: PageData;
  $: projects = data.projects;

  type Filters = Pick<ProjectFilters, 'projectSearch' | 'projectType'>;
  enum ViewMode {
    Table = 'table',
    Grid = 'grid',
  }

  const { queryParamValues: filters, defaultQueryParamValues: defaultFilterValues } = getSearchParams<Filters>({
    projectSearch: queryParam.string<string>(''),
    projectType: queryParam.string<ProjectType | undefined>(undefined),
  });

  let initializedMode = false;
  let mode: ViewMode;
  $: defaultMode = $projects.length < 10 ? ViewMode.Grid : ViewMode.Table;
  $: STORAGE_VIEW_MODE_KEY = 'projectViewMode';

  $: {
    if (!initializedMode) {
      const storedMode = browser && localStorage?.getItem(STORAGE_VIEW_MODE_KEY);
      if (storedMode === ViewMode.Table || storedMode === ViewMode.Grid) {
        mode = storedMode;
      } else {
        mode = defaultMode;
      }
      initializedMode = true;
    }
  }

  function selectMode(selectedMode: ViewMode): void {
    mode = selectedMode;
    localStorage.setItem(STORAGE_VIEW_MODE_KEY, mode);
  }

  let filteredProjects: ProjectItem[] = [];
  let limitResults = true;
  let hasActiveFilter = false;
  $: shownProjects = limitResults ? limit(filteredProjects) : filteredProjects;
</script>

<HeaderPage wide title={$t('user_dashboard.title')}>
  <svelte:fragment slot="header-content">
    <div class="flex gap-4 w-full">
      <div class="grow">
        <ProjectFilter
          {filters}
          filterDefaults={defaultFilterValues}
          projects={$projects}
          bind:filteredProjects
          bind:hasActiveFilter
          on:change={() => (limitResults = true)}
          filterKeys={['projectSearch', 'projectType']}
        />
      </div>
      <div class="join">
        <IconButton
          icon="i-mdi-grid"
          join
          active={mode === ViewMode.Grid}
          on:click={() => selectMode(ViewMode.Grid)} />
        <IconButton
          icon="i-mdi-land-rows-horizontal"
          join
          active={mode === ViewMode.Table}
          on:click={() => selectMode(ViewMode.Table)} />
      </div>
    </div>
  </svelte:fragment>
  <svelte:fragment slot="actions">
    {#if data.user.emailVerified}
      <a href="/project/create" class="btn btn-success">
        <span class="i-mdi-plus text-2xl" />
        {$t('project.create.title')}
      </a>
    {/if}
  </svelte:fragment>

  {#if !data.user.emailVerified || !$projects.length}
    <div class="text-lg text-secondary flex gap-4 items-center justify-center">
      <span class="i-mdi-creation-outline text-xl shrink-0" />
      {#if !data.user.emailVerified}
        {$t('user_dashboard.not_verified')}
      {:else}
        {$t('user_dashboard.no_projects')}
      {/if}
    </div>
  {:else}
    {#if mode === ViewMode.Grid}
      <ProjectList projects={shownProjects} />
    {:else}
      <ProjectTable projects={shownProjects} columns={['name', 'code', 'users', 'type', 'lastChange']} />
    {/if}

    {#if shownProjects.length < filteredProjects.length}
      <Button class="float-right mt-2" on:click={() => (limitResults = false)}>
        {$t('paging.load_more')}
      </Button>
    {/if}
  {/if}
</HeaderPage>
