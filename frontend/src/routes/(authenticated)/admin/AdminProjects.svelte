<script lang="ts">
  import { Badge } from '$lib/components/Badges';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { DEFAULT_PAGE_SIZE, limit } from '$lib/components/Paging';
  import { ProjectFilter, ProjectTable, type ProjectItem, filterProjects } from '$lib/components/Projects';
  import { RefineFilterMessage } from '$lib/components/Table';
  import { DialogResponse } from '$lib/components/modals';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import { Button } from '$lib/forms';
  import { _deleteProject } from '$lib/gql/mutations';
  import t from '$lib/i18n';
  import { TrashIcon } from '$lib/icons';
  import { useNotifications } from '$lib/notify';
  import type { QueryParams } from '$lib/util/query-params';
  import type { AdminSearchParams } from './+page';

  export let projects: ProjectItem[];
  export let queryParams: QueryParams<AdminSearchParams>;
  $: filters = queryParams.queryParamValues;
  $: filterDefaults = queryParams.defaultQueryParamValues;

  const { notifyWarning } = useNotifications();

  let filteredProjects: ProjectItem[] = [];
  let limitResults = true;
  let hasActiveFilter = false;
  $: filteredProjects = filterProjects(projects, $filters);
  $: shownProjects = limitResults ? limit(filteredProjects, hasActiveFilter ? DEFAULT_PAGE_SIZE : 10) : filteredProjects;

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
</script>

<ConfirmDeleteModal bind:this={deleteProjectModal} i18nScope="delete_project_modal" />
<div>
  <div class="flex justify-between items-center">
    <span class="text-xl flex gap-4">
      {$t('admin_dashboard.project_table_title')}
      <Badge>
        <span class="inline-flex gap-2">
          {shownProjects.length}
          <span>/</span>
          {filteredProjects.length}
        </span>
      </Badge>
    </span>
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
    />
  </div>

  <div class="divider" />

  <ProjectTable projects={shownProjects}>
    <td class="p-0" slot="actions" let:project>
      {#if !project.deletedDate}
        <Dropdown>
          <!-- svelte-ignore a11y-label-has-associated-control -->
          <label tabindex="-1" class="btn btn-ghost btn-square">
            <span class="i-mdi-dots-vertical text-lg" />
          </label>
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
    {#if hasActiveFilter}
      <Button class="float-right mt-2" on:click={() => (limitResults = false)}>
        {$t('paging.load_more')}
      </Button>
    {:else}
      <RefineFilterMessage total={filteredProjects.length} showing={shownProjects.length} />
    {/if}
  {/if}
</div>
