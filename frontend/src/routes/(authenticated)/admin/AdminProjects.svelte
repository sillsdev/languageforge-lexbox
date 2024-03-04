<script lang="ts">
  import { navigating } from '$app/stores';
  import { Badge } from '$lib/components/Badges';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { DEFAULT_PAGE_SIZE, limit } from '$lib/components/Paging';
  import { ProjectFilter, ProjectTable, type ProjectItem, filterProjects, type ProjectFilters } from '$lib/components/Projects';
  import { RefineFilterMessage } from '$lib/components/Table';
  import { DialogResponse } from '$lib/components/modals';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import { Button } from '$lib/forms';
  import { _deleteProject } from '$lib/gql/mutations';
  import t, { number } from '$lib/i18n';
  import { TrashIcon } from '$lib/icons';
  import { useNotifications } from '$lib/notify';
  import type { QueryParams } from '$lib/util/query-params';
  import { derived } from 'svelte/store';
  import type { AdminSearchParams } from './+page';
  import DevContent from '$lib/layout/DevContent.svelte';

  export let projects: ProjectItem[];
  export let queryParams: QueryParams<AdminSearchParams>;
  $: filters = queryParams.queryParamValues;
  $: filterDefaults = queryParams.defaultQueryParamValues;

  const { notifyWarning, notifySuccess } = useNotifications();

  const serverSideProjectFilterKeys = (['showDeletedProjects'] as const satisfies Readonly<(keyof ProjectFilters)[]>);

  const loading = derived(navigating, (nav) => {
    const fromUrl = nav?.from?.url;
    return fromUrl && serverSideProjectFilterKeys.some((key) =>
      (fromUrl.searchParams.get(key) ?? filterDefaults?.[key])?.toString() !== $filters?.[key]?.toString());
  });

  let filteredProjects: ProjectItem[] = [];
  let limitResults = true;
  let hasActiveFilter = false;
  let lastLoadUsedActiveFilter = false;
  $: if (!$loading) lastLoadUsedActiveFilter = hasActiveFilter;
  $: filteredProjects = filterProjects(projects, $filters);
  $: shownProjects = limitResults ? limit(filteredProjects, lastLoadUsedActiveFilter ? DEFAULT_PAGE_SIZE : 10) : filteredProjects;

  let deleteProjectModal: ConfirmDeleteModal;
  async function softDeleteProject(project: ProjectItem): Promise<void> {
    const result = await deleteProjectModal.open(project.name, async () => {
      const { error } = await _deleteProject(project.id);
      return error?.message;
    });
    if (result.response === DialogResponse.Submit) {
      notifyWarning($t('delete_project_modal.success', { name: project.name, code: project.code }));
    }
  }

  async function updateAllLexEntryCounts(): Promise<void> {
    const result = await fetch(`/api/project/updateAllLexEntryCounts?onlyUnknown=true`, {method: 'POST'});
    const count = await result.text();
    notifySuccess(`${count} projects updated` + (Number(count) == 0 ? `. You're all done!` : ''));
  }
</script>

<ConfirmDeleteModal bind:this={deleteProjectModal} i18nScope="delete_project_modal" />
<div>
  <div class="flex justify-between items-center">
    <h2 class="text-2xl flex gap-4 items-end">
      {$t('admin_dashboard.project_table_title')}
      <Badge>
        <span class="inline-flex gap-2">
          {$number(shownProjects.length)}
          <span>/</span>
          {$number(filteredProjects.length)}
        </span>
      </Badge>
    </h2>
    <a href="/project/create" class="btn btn-sm btn-success">
      <span class="max-sm:hidden">
        {$t('project.create.title')}
      </span>
      <span class="i-mdi-plus text-2xl" />
    </a>
  </div>

  <div class="mt-4">
    <ProjectFilter
      {filters}
      {filterDefaults}
      bind:hasActiveFilter
      on:change={() => (limitResults = true)}
      loading={$loading}
    />
  </div>

  <div class="divider" />

  <ProjectTable projects={shownProjects}>
    <td class="p-0" slot="actions" let:project>
      {#if !project.deletedDate}
        <Dropdown>
          <button class="btn btn-ghost btn-square">
            <span class="i-mdi-dots-vertical text-lg" />
          </button>
          <ul slot="content" class="menu">
            <li>
              <button class="text-error whitespace-nowrap" on:click={() => softDeleteProject(project)}>
                <TrashIcon />
                {$t('delete_project_modal.submit')}
              </button>
            </li>
          </ul>
        </Dropdown>
      {/if}
    </td>
  </ProjectTable>

  {#if shownProjects.length < filteredProjects.length}
    {#if lastLoadUsedActiveFilter}
      <Button class="float-right mt-2" on:click={() => (limitResults = false)}>
        {$t('paging.load_more')}
      </Button>
    {:else}
      <RefineFilterMessage total={filteredProjects.length} showing={shownProjects.length} />
    {/if}
  {/if}

<DevContent>
  <p><span class="text-bold">TEMPORARY:</span> <button class="btn btn-warning" on:click={updateAllLexEntryCounts}> Update all lex entry counts </button>
</DevContent>
</div>
