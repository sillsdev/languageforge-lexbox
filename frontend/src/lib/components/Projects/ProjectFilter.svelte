<script module lang="ts">
  import { type Project, type ProjectType } from '$lib/gql/types';
  import type { DraftProject } from '../../../routes/(authenticated)/admin/+page';

  export type ProjectItem = Pick<Project, 'id' | 'name' | 'code' | 'type'> & Partial<Project>;
  export type ProjectItemWithDraftStatus =
    | (ProjectItem & { isDraft?: false })
    | (DraftProject & { isDraft: true; createUrl: string });

  export type Confidentiality = `${boolean}` | 'unset';

  export type ProjectFilters = {
    projectSearch: string;
    projectType: ProjectType | undefined;
    confidential: Confidentiality | undefined;
    showDeletedProjects: boolean;
    hideDraftProjects: boolean;
    emptyProjects: boolean;
    memberSearch: string | undefined;
  };

  export function filterProjects(
    projects: ProjectItemWithDraftStatus[],
    projectFilters: Partial<ProjectFilters>,
  ): ProjectItemWithDraftStatus[] {
    const searchLower = projectFilters.projectSearch?.toLocaleLowerCase();
    return projects.filter(
      (p) =>
        (!searchLower ||
          p.name.toLocaleLowerCase().includes(searchLower) ||
          p.code.toLocaleLowerCase().includes(searchLower)) &&
        (!projectFilters.projectType || p.type === projectFilters.projectType) &&
        (!projectFilters.hideDraftProjects || !p.isDraft) &&
        (!projectFilters.emptyProjects || p.isDraft || !p.lastCommit) &&
        (projectFilters.confidential === undefined ||
          projectFilters.confidential === p.isConfidential?.toString() ||
          (projectFilters.confidential === 'unset' && (p.isConfidential ?? undefined) === undefined)),
    );
  }
</script>

<script lang="ts">
  import { FormField, ProjectTypeSelect } from '$lib/forms';
  import type { Writable } from 'svelte/store';
  import { ProjectTypeIcon } from '../ProjectType';
  import ActiveFilter from '../FilterBar/ActiveFilter.svelte';
  import FilterBar, { type OnFiltersChanged } from '../FilterBar/FilterBar.svelte';
  import { AuthenticatedUserIcon, Icon, TrashIcon } from '$lib/icons';
  import t from '$lib/i18n';
  import IconButton from '../IconButton.svelte';
  import ProjectConfidentialityFilterSelect from './ProjectConfidentialityFilterSelect.svelte';
  import SupHelp from '../help/SupHelp.svelte';
  import { helpLinks } from '../help';
  import BypassCloudflareEmailObfuscation from '$lib/components/BypassCloudflareEmailObfuscation.svelte';

  type Filters = Partial<ProjectFilters> & Pick<ProjectFilters, 'projectSearch'>;
  interface Props {
    filters: Writable<Filters>;
    filterDefaults: Filters;
    onFiltersChanged?: OnFiltersChanged;
    hasActiveFilter?: boolean;
    autofocus?: true;
    filterKeys?: (keyof Filters)[];
    loading?: boolean;
  }

  let {
    filters,
    filterDefaults,
    onFiltersChanged,
    hasActiveFilter = $bindable(false),
    autofocus,
    filterKeys = [
      'projectSearch',
      'projectType',
      'confidential',
      'showDeletedProjects',
      'memberSearch',
      'hideDraftProjects',
      'emptyProjects',
    ],
    loading = false,
  }: Props = $props();

  function filterEnabled(filter: keyof Filters): boolean {
    return filterKeys.includes(filter);
  }
</script>

<FilterBar
  {onFiltersChanged}
  searchKey="projectSearch"
  {autofocus}
  {filters}
  {filterDefaults}
  bind:hasActiveFilter
  {filterKeys}
  {loading}
>
  {#snippet activeFilterSlot({ activeFilters })}
    {#each activeFilters as filter}
      {#if filter.key === 'projectType'}
        <ActiveFilter {filter}>
          <ProjectTypeIcon type={filter.value} />
        </ActiveFilter>
      {:else if filter.key === 'confidential' && filter.value}
        <ActiveFilter {filter}>
          {#if filter.value === 'true'}
            <Icon icon="i-mdi-shield-lock-outline" color="text-warning" />
            {$t('project.confidential.confidential')}
          {:else if filter.value === 'false'}
            <Icon icon="i-mdi-shield-lock-open-outline" />
            {$t('project.confidential.not_confidential')}
          {:else}
            <Icon icon="i-mdi-shield-lock-outline" color="text-warning" />
            {$t('project.confidential.unspecified')}
          {/if}
        </ActiveFilter>
      {:else if filter.key === 'showDeletedProjects' && filter.value}
        <ActiveFilter {filter}>
          <TrashIcon color="text-error" />
          {$t('project.filter.show_deleted')}
        </ActiveFilter>
      {:else if filter.key === 'memberSearch' && filter.value}
        <ActiveFilter {filter}>
          <AuthenticatedUserIcon />
          <BypassCloudflareEmailObfuscation>
            {filter.value}
          </BypassCloudflareEmailObfuscation>
        </ActiveFilter>
      {:else if filter.key === 'hideDraftProjects' && filter.value}
        <ActiveFilter {filter}>
          <Icon icon="i-mdi-script" color="text-warning" />
          {$t('project.filter.hide_drafts')}
        </ActiveFilter>
      {:else if filter.key === 'emptyProjects' && filter.value}
        <ActiveFilter {filter}>
          <Icon icon="i-mdi-file-hidden" />
          {$t('project.filter.empty')}
        </ActiveFilter>
      {/if}
    {/each}
  {/snippet}
  {#snippet filterSlot()}
    <h2 class="card-title">{$t('project.filter.title')}</h2>
    {#if filterEnabled('memberSearch')}
      <FormField label={$t('project.filter.project_member')}>
        {#if $filters.memberSearch}
          <div class="join">
            <input class="input input-bordered join-item flex-grow" readonly value={$filters.memberSearch} />
            <div class="join-item isolate">
              <IconButton icon="i-mdi-close" onclick={() => ($filters.memberSearch = undefined)} />
            </div>
          </div>
        {:else}
          <div class="alert alert-info gap-2">
            <Icon icon="i-mdi-information-outline" size="text-2xl" />
            <div>
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
        <ProjectTypeSelect bind:value={$filters.projectType} undefinedOptionLabel={$t('common.any')} includeUnknown />
      </div>
    {/if}
    {#if filterEnabled('confidential')}
      <div class="form-control">
        <ProjectConfidentialityFilterSelect bind:value={$filters.confidential} />
      </div>
    {/if}
    {#if filterEnabled('showDeletedProjects')}
      <div class="form-control">
        <label class="cursor-pointer label gap-4">
          <span class="label-text inline-flex items-center gap-2">
            {$t('project.filter.show_deleted')}
            <TrashIcon color="text-error" />
          </span>
          <input bind:checked={$filters.showDeletedProjects} type="checkbox" class="toggle toggle-error" />
        </label>
      </div>
    {/if}
    {#if filterEnabled('hideDraftProjects')}
      <div class="form-control">
        <label class="cursor-pointer label gap-4">
          <span class="label-text inline-flex items-center gap-2">
            <span>
              {$t('project.filter.hide_drafts')}
              <SupHelp helpLink={helpLinks.projectRequest} />
            </span>
            <Icon icon="i-mdi-script" color="text-warning" />
          </span>
          <input bind:checked={$filters.hideDraftProjects} type="checkbox" class="toggle toggle-warning" />
        </label>
      </div>
    {/if}
    {#if filterEnabled('emptyProjects')}
      <div class="form-control">
        <label class="cursor-pointer label gap-4">
          <span class="label-text inline-flex items-center gap-2">
            {$t('project.filter.show_empty')}
            <Icon icon="i-mdi-file-hidden" />
          </span>
          <input bind:checked={$filters.emptyProjects} type="checkbox" class="toggle toggle-warning" />
        </label>
      </div>
    {/if}
  {/snippet}
</FilterBar>
