<script context="module" lang="ts">
  import { type Project, type ProjectType } from '$lib/gql/types';

  export type ProjectItem = Pick<Project, 'id' | 'name' | 'code' | 'type'> & Partial<Project>;

  export type ProjectFilters = {
    projectSearch: string;
    projectType: ProjectType | undefined;
    showDeletedProjects: boolean;
    userEmail: string | undefined;
  };

  export function filterProjects(
    projects: ProjectItem[],
    projectFilters: Partial<ProjectFilters>,
  ): ProjectItem[] {
    const searchLower = projectFilters.projectSearch?.toLocaleLowerCase();
    return projects.filter(
      (p) =>
        (!searchLower ||
          p.name.toLocaleLowerCase().includes(searchLower) ||
          p.code.toLocaleLowerCase().includes(searchLower)) &&
        (!projectFilters.projectType || p.type === projectFilters.projectType),
    );
  }
</script>

<script lang="ts">
  import { FormField, ProjectTypeSelect } from '$lib/forms';
  import type { Writable } from 'svelte/store';
  import { ProjectTypeIcon } from '../ProjectType';
  import ActiveFilter from '../FilterBar/ActiveFilter.svelte';
  import FilterBar from '../FilterBar/FilterBar.svelte';
  import { AuthenticatedUserIcon, TrashIcon } from '$lib/icons';
  import t from '$lib/i18n';
  import IconButton from '../IconButton.svelte';

  type Filters = Partial<ProjectFilters> & Pick<ProjectFilters, 'projectSearch'>;
  export let filters: Writable<Filters>;
  export let filterDefaults: Filters;
  export let hasActiveFilter: boolean = false;
  export let autofocus: true | undefined = undefined;
  export let filterKeys: (keyof Filters)[] = ['projectSearch', 'projectType', 'showDeletedProjects', 'userEmail'];
  export let loading = false;

  function filterEnabled(filter: keyof Filters): boolean {
    return filterKeys.includes(filter);
  }
</script>

<FilterBar on:change searchKey="projectSearch" {autofocus} {filters} {filterDefaults} bind:hasActiveFilter {filterKeys} {loading}>
  <svelte:fragment slot="activeFilters" let:activeFilters>
    {#each activeFilters as filter}
      {#if filter.key === 'projectType'}
        <ActiveFilter {filter}>
          <ProjectTypeIcon type={filter.value} />
        </ActiveFilter>
      {:else if filter.key === 'showDeletedProjects'}
        <ActiveFilter {filter}>
          <TrashIcon color="text-error" />
          {$t('project.filter.show_deleted')}
        </ActiveFilter>
      {:else if filter.key === 'userEmail' && filter.value}
        <ActiveFilter {filter}>
          <AuthenticatedUserIcon />
          {filter.value}
        </ActiveFilter>
      {/if}
    {/each}
  </svelte:fragment>
  <svelte:fragment slot="filters">
    <h2 class="card-title">{$t('project.filter.title')}</h2>
    {#if filterEnabled('userEmail')}
      <FormField label={$t('project.filter.project_member')}>
        {#if $filters.userEmail}
          <div class="join">
            <input
              class="input input-bordered join-item flex-grow"
              readonly
              value={$filters.userEmail}
            />
            <div class="join-item isolate">
              <IconButton icon="i-mdi-close" on:click={() => ($filters.userEmail = undefined)} />
            </div>
          </div>
        {:else}
          <div class="alert alert-info gap-2">
            <span class="i-mdi-info-outline text-xl"></span>
            <div class="flex_ items-center gap-2">
              <span class="mr-1">{$t('project.filter.select_user_from_table')}</span>
              <span class="btn btn-sm btn-square pointer-events-none">
                <span class="i-mdi-dots-vertical"></span>
              </span>
              <span class="i-mdi-chevron-right"></span>
              <span class="btn btn-sm pointer-events-none normal-case font-normal">
                <span class="i-mdi-filter-outline mr-1"></span>
                {$t('project.filter.filter_user_projects')}
              </span>
            </div>
          </div>
        {/if}
      </FormField>
    {/if}
    {#if filterEnabled('projectType')}
      <div class="form-control">
        <ProjectTypeSelect bind:value={$filters.projectType} undefinedOptionLabel={$t('project_type.any')} />
      </div>
    {/if}
    {#if filterEnabled('showDeletedProjects')}
      <div class="form-control">
        <label class="cursor-pointer label gap-4">
          <span class="label-text">{$t('project.filter.show_deleted')}</span>
          <input bind:checked={$filters.showDeletedProjects} type="checkbox" class="toggle toggle-error" />
        </label>
      </div>
    {/if}
  </svelte:fragment>
</FilterBar>
