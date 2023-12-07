<script lang="ts">
  import type { PageData } from './$types';
  import t from '$lib/i18n';
  import ProjectList from '$lib/components/ProjectList.svelte';
  import { Page } from '$lib/layout';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import type { ProjectType } from '$lib/gql/types';
  import { ProjectFilter, type ProjectFilters, type ProjectItem } from '$lib/components/Projects';
  import ProjectTable from '$lib/components/Projects/ProjectTable.svelte';
  import { Button } from '$lib/forms';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { browser } from '$app/environment';
  import { limit } from '$lib/components/Paging';

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
  $: otherMode = mode === ViewMode.Grid ? ViewMode.Table : ViewMode.Grid;
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

<Page wide>
  <svelte:fragment slot="header">
    {$t('user_dashboard.title')}
  </svelte:fragment>

  <div class="flex gap-4">
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

    <Dropdown>
      <!-- svelte-ignore a11y-label-has-associated-control -->
      <label tabindex="-1" class="btn btn-square">
        <span class="i-mdi-dots-vertical text-lg" />
      </label>
      <ul slot="content" class="menu" let:closeDropdown>
        {#if data.user.emailVerified}
          <li>
            <a href="/project/create" class="text-success whitespace-nowrap">
              <span class="i-mdi-plus text-2xl" />
              {$t('project.create.title')}
            </a>
          </li>
        {/if}
        <li>
          <button
            on:click={() => {
              closeDropdown();
              selectMode(otherMode);
            }}
            class="whitespace-nowrap"
          >
            <span
              class:i-mdi-land-rows-horizontal={otherMode === ViewMode.Table}
              class:i-mdi-grid={otherMode === ViewMode.Grid}
              class="text-xl"
            />
            {$t('user_dashboard.use_view', { mode: otherMode })}
          </button>
        </li>
      </ul>
    </Dropdown>
  </div>

  <div class="divider" />

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
      <ProjectTable projects={shownProjects} columns={['name', 'code', 'users', 'type']} />
    {/if}

    {#if shownProjects.length < filteredProjects.length}
      <Button class="float-right mt-2" on:click={() => (limitResults = false)}>
        {$t('paging.load_more')}
      </Button>
    {/if}
  {/if}
</Page>
