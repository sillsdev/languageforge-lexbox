<script lang="ts">
  import { run } from 'svelte/legacy';

  import type { PageData } from './$types';
  import t from '$lib/i18n';
  import ProjectList from '$lib/components/ProjectList.svelte';
  import { HeaderPage } from '$lib/layout';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import type { ProjectType } from '$lib/gql/types';
  import {
    ProjectFilter,
    filterProjects,
    type ProjectFilters,
    type ProjectItemWithDraftStatus,
  } from '$lib/components/Projects';
  import ProjectTable from '$lib/components/Projects/ProjectTable.svelte';
  import { Button } from '$lib/forms';
  import { limit } from '$lib/components/Paging';
  import IconButton from '$lib/components/IconButton.svelte';
  import Cookies from 'js-cookie';
  import { STORAGE_VIEW_MODE_KEY, ViewMode } from './shared';

  interface Props {
    data: PageData;
  }

  let { data }: Props = $props();
  let projects = $derived(data.projects);
  let draftProjects = $derived(data.draftProjects);

  type Filters = Pick<ProjectFilters, 'projectSearch' | 'projectType'>;

  const { queryParamValues: filters, defaultQueryParamValues: defaultFilterValues } = getSearchParams<Filters>({
    projectSearch: queryParam.string<string>(''),
    projectType: queryParam.string<ProjectType | undefined>(undefined),
  });

  let allProjects: ProjectItemWithDraftStatus[] = $state([]);
  let filteredProjects: ProjectItemWithDraftStatus[] = $state([]);
  let limitResults = $state(true);
  run(() => {
    allProjects = [
      ...$draftProjects.map((p) => ({
        ...p,
        isDraft: true as const,
        createUrl: '',
      })),
      ...$projects.map((p) => ({ ...p, isDraft: false as const })),
    ];
  });
  run(() => {
    filteredProjects = filterProjects(allProjects, $filters);
  });
  let shownProjects = $derived(limitResults ? limit(filteredProjects) : filteredProjects);

  // TODO: This whole thing with initializedMode, defaultMode, and mode *and* the run() block below
  // looks like it could be simplified, perhaps by turning it into a single $derived.by
  let initializedMode = $state(false);
  let defaultMode = $derived(allProjects.length < 10 ? ViewMode.Grid : ViewMode.Table);
  let mode: ViewMode = $state(defaultMode); // Only captures initial value, but that's okay; rest is captured in run() block

  run(() => {
    if (!initializedMode) {
      const storedMode = data.projectViewMode;
      if (storedMode === ViewMode.Table || storedMode === ViewMode.Grid) {
        mode = storedMode;
      } else {
        mode = defaultMode;
      }
      initializedMode = true;
    }
  });

  function selectMode(selectedMode: ViewMode): void {
    mode = selectedMode;
    Cookies.set(STORAGE_VIEW_MODE_KEY, mode, { expires: 365 * 10 });
  }
</script>

<HeaderPage wide titleText={$t('user_dashboard.title')}>
  {#snippet headerContent()}
    <div class="flex gap-4 w-full">
      <div class="grow">
        <ProjectFilter
          {filters}
          filterDefaults={defaultFilterValues}
          on:change={() => (limitResults = true)}
          filterKeys={['projectSearch', 'projectType', 'confidential']}
        />
      </div>
      <div class="join">
        <IconButton icon="i-mdi-grid" join active={mode === ViewMode.Grid} on:click={() => selectMode(ViewMode.Grid)} />
        <IconButton
          icon="i-mdi-land-rows-horizontal"
          join
          active={mode === ViewMode.Table}
          on:click={() => selectMode(ViewMode.Table)}
        />
      </div>
    </div>
  {/snippet}
  {#snippet actions()}
    {#if data.user.emailVerified && !data.user.createdByAdmin}
      <a href="/project/create" class="btn btn-success">
        {$t('project.create.title')}
        <span class="i-mdi-plus text-2xl"></span>
      </a>
    {/if}
  {/snippet}

  {#if !allProjects.length}
    <div class="text-lg text-secondary flex gap-4 items-center justify-center">
      <span class="i-mdi-creation-outline text-xl shrink-0"></span>
      {#if !data.user.emailVerified && !data.user.createdByAdmin}
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
