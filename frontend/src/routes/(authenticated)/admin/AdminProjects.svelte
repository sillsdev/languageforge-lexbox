<script lang="ts">
  import {navigating} from '$app/stores';
  import {Badge} from '$lib/components/Badges';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import {DEFAULT_PAGE_SIZE, limit} from '$lib/components/Paging';
  import {
    filterProjects,
    ProjectFilter,
    type ProjectFilters,
    type ProjectItem,
    type ProjectItemWithDraftStatus,
    ProjectTable
  } from '$lib/components/Projects';
  import {RefineFilterMessage} from '$lib/components/Table';
  import {DialogResponse} from '$lib/components/modals';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import {Button} from '$lib/forms';
  import {_deleteProject, _deleteDraftProject} from '$lib/gql/mutations';
  import t, {number} from '$lib/i18n';
  import {TrashIcon} from '$lib/icons';
  import {useNotifications} from '$lib/notify';
  import {type QueryParams, toSearchParams} from '$lib/util/query-params';
  import {derived} from 'svelte/store';
  import type {AdminSearchParams, DraftProject} from './+page';
  import AdminTabs from './AdminTabs.svelte';
  import type {CreateProjectInput} from '$lib/gql/types';

  export let projects: ProjectItem[];
  export let draftProjects: DraftProject[];
  export let queryParams: QueryParams<AdminSearchParams>;
  $: queryParamValues = queryParams.queryParamValues;
  $: filters = queryParamValues;
  $: filterDefaults = queryParams.defaultQueryParamValues;

  const { notifyWarning } = useNotifications();

  const serverSideProjectFilterKeys = (['showDeletedProjects'] as const satisfies Readonly<(keyof ProjectFilters)[]>);

  const loading = derived(navigating, (nav) => {
    const fromUrl = nav?.from?.url;
    return fromUrl && serverSideProjectFilterKeys.some((key) =>
      (fromUrl.searchParams.get(key) ?? filterDefaults?.[key])?.toString() !== $filters?.[key]?.toString());
  });

  let allProjects: ProjectItemWithDraftStatus[] = [];
  let filteredProjects: ProjectItemWithDraftStatus[] = [];
  let limitResults = true;
  let hasActiveFilter = false;
  let lastLoadUsedActiveFilter = false;
  $: if (!$loading) lastLoadUsedActiveFilter = hasActiveFilter;
  $: allProjects = [
    ...draftProjects.map(p => ({
      ...p, isDraft: true as const,
      createUrl: `/project/create?${toSearchParams<CreateProjectInput>(p as CreateProjectInput)}` /* TODO #737 - Remove unnecessary cast */
    })),
    ...projects.map(p => ({ ...p, isDraft: false as const })),
  ];
  $: filteredProjects = filterProjects(allProjects, $filters);
  $: shownProjects = limitResults ? limit(filteredProjects, lastLoadUsedActiveFilter ? DEFAULT_PAGE_SIZE : 10) : filteredProjects;

  let deleteProjectModal: ConfirmDeleteModal;
  async function deleteProjectOrDraft(project: ProjectItemWithDraftStatus): Promise<void> {
    const deleteFn = project.isDraft ? _deleteProject : _deleteDraftProject;
    const result = await deleteProjectModal.open(project.name, async () => {
      const { error } = await deleteFn(project.id);
      return error?.message;
    });
    if (result.response === DialogResponse.Submit) {
      notifyWarning($t('delete_project_modal.success', { name: project.name, code: project.code }));
    }
  }
</script>

<ConfirmDeleteModal bind:this={deleteProjectModal} i18nScope="delete_project_modal" />
<div>
  <AdminTabs activeTab="projects" on:clickTab={(event) => $queryParamValues.tab = event.detail}>
    <div class="flex gap-4 justify-between grow">
      <div class="flex gap-4 items-center">
        {$t('admin_dashboard.project_table_title')}
        <div class="contents max-xs:hidden">
          <Badge>
            <span class="inline-flex gap-2">
              {$number(shownProjects.length)}
              <span>/</span>
              {$number(filteredProjects.length)}
            </span>
          </Badge>
        </div>
      </div>
      <a class="btn btn-sm btn-success max-xs:btn-square"
        href="/project/create">
        <span class="admin-tabs:hidden">
          {$t('project.create.title')}
        </span>
        <span class="i-mdi-plus text-2xl" />
      </a>
    </div>
  </AdminTabs>

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
      {#if project.isDraft || !project.deletedDate}
        <Dropdown>
          <button class="btn btn-ghost btn-square">
            <span class="i-mdi-dots-vertical text-lg" />
          </button>
          <ul slot="content" class="menu">
            <li>
              <button class="text-error whitespace-nowrap" on:click={() => deleteProjectOrDraft(project)}>
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
</div>
