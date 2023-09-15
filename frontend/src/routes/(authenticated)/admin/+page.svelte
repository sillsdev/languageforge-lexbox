<script lang="ts">
  import { Badge } from '$lib/components/Badges';
  import FormatDate from '$lib/components/FormatDate.svelte';
  import { ProjectTypeIcon } from '$lib/components/ProjectType';
  import Input from '$lib/forms/Input.svelte';
  import t from '$lib/i18n';
  import type { PageData } from './$types';
  import DeleteUserModal from '$lib/components/DeleteUserModal.svelte';
  import EditUserAccount from './EditUserAccount.svelte';
  import type { LoadAdminDashboardQuery, ProjectType } from '$lib/gql/types';
  import { notifySuccess, notifyWarning } from '$lib/notify';
  import { DialogResponse } from '$lib/components/modals';
  import { Duration } from '$lib/util/time';
  import { Icon, TrashIcon } from '$lib/icons';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import { _deleteProject } from '$lib/gql/mutations';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import Button from '$lib/forms/Button.svelte';
  import { getProjectTypeI18nKey } from '$lib/components/ProjectType';
  import { FormField, ProjectTypeSelect } from '$lib/forms';
  import { getSearchParams, queryParam  } from '$lib/util/query-params';
  import AuthenticatedUserIcon from '$lib/icons/AuthenticatedUserIcon.svelte';
  import IconButton from '$lib/components/IconButton.svelte';
  import type { AdminSearchParams } from './+page';
  import FilterBar from '$lib/components/FilterBar/FilterBar.svelte';
  import { ActiveFilter } from '$lib/components/FilterBar';
  import { bubbleFocusOnDestroy } from '$lib/util/focus';

  type UserRow = LoadAdminDashboardQuery['users'][0];
  type ProjectRow = LoadAdminDashboardQuery['projects'][0];

  export let data: PageData;
  $: allProjects = data.projects;
  $: allUsers = data.users;

  const { queryParams, defaultQueryParams } = getSearchParams<AdminSearchParams>({
    showDeletedProjects: queryParam.boolean<boolean>(false),
    projectType: queryParam.string<ProjectType | undefined>(undefined),
    userEmail: queryParam.string(undefined),
    projectSearch: queryParam.string<string>(''),
  });

  function filterProjectsByUser(user: UserRow): void {
    $queryParams.userEmail = user.email;
  }

  let deleteUserModal: DeleteUserModal;
  let deleteProjectModal: ConfirmDeleteModal;
  let formModal: EditUserAccount;

  async function deleteUser(user: UserRow): Promise<void> {
    formModal.close();
    const { response } = await deleteUserModal.open(user);
    if (response == DialogResponse.Submit) {
      notifyWarning($t('admin_dashboard.notifications.user_deleted', { name: user.name }));
    }
  }

  async function openModal(user: UserRow): Promise<void> {
    const { response, formState } = await formModal.openModal(user);
    if (response == DialogResponse.Submit) {
      if (formState.name.tainted || formState.password.tainted || formState.role.tainted) {
        notifySuccess($t('admin_dashboard.notifications.user_updated', { name: formState.name.currentValue }));
      }
      if (formState.email.changed) {
        notifySuccess(
          $t('admin_dashboard.notifications.email_need_verification', {
            name: user.name,
            requestedEmail: formState.email.currentValue,
          }),
          Duration.Long
        );
      }
    }
  }

  const defaultFilterLimit = 100;

  function getFilteredUser(userEmail: string | undefined): UserRow | undefined {
    return !userEmail ? undefined
    : filteredUser && filteredUser.email == $queryParams.userEmail ? filteredUser
    : $allUsers.find(user => user.email === $queryParams.userEmail);
  }

  let hasActiveProjectFilter: boolean;

  let userSearch = '';
  $: projectSearchLower = $queryParams.projectSearch.toLocaleLowerCase();
  let projectFilterLimit = defaultFilterLimit;
  $: projectLimit = hasActiveProjectFilter ? projectFilterLimit : 10;
  $: filteredUser = getFilteredUser($queryParams.userEmail);
  $: userProjects = !filteredUser ? undefined
    : filteredUser.projects.map(({projectId}) => $allProjects.find(p => p.id === projectId) as ProjectRow);
  $: filteredProjects = (userProjects ?? $allProjects).filter(
    (p) =>
      (!$queryParams.projectSearch ||
        p.name.toLocaleLowerCase().includes(projectSearchLower) ||
        p.code.toLocaleLowerCase().includes(projectSearchLower)) &&
      (!$queryParams.projectType || p.type === $queryParams.projectType));
  $: projects = filteredProjects.slice(0, projectLimit);
  $: {
    // Reset limit if search is changed
    hasActiveProjectFilter;
    projectFilterLimit = defaultFilterLimit;
  }

  $: userSearchLower = userSearch.toLocaleLowerCase();
  let userSearchLimit = defaultFilterLimit;
  $: userLimit = userSearch ? userSearchLimit : 10;
  $: filteredUsers = $allUsers.filter(
    (u) =>
      !userSearch ||
      u.name.toLocaleLowerCase().includes(userSearchLower) ||
      u.email.toLocaleLowerCase().includes(userSearchLower)
  );
  $: users = filteredUsers.slice(0, userLimit);
  $: {
    // Reset limit if search is changed
    userSearch;
    userSearchLimit = defaultFilterLimit;
  }

  async function softDeleteProject(project: (typeof projects)[0]): Promise<void> {
    const result = await deleteProjectModal.open(project.name, async () => {
      const { error } = await _deleteProject(project.id);
      return error?.message;
    });
    if (result.response === DialogResponse.Submit) {
      notifyWarning($t('delete_project_modal.success', { name: project.name, code: project.code }));
    }
  }
</script>

<svelte:head>
  <title>{$t('admin_dashboard.title')}</title>
</svelte:head>
<main class="flex justify-center">
  <div class="grid lg:grid-cols-2 grid-cols-1 gap-10">
    <div>
      <div class="flex justify-between items-center">
        <span class="text-xl flex gap-4">
          {$t('admin_dashboard.project_table_title')}
          <Badge>
            <span class="inline-flex gap-2">
              {hasActiveProjectFilter ? filteredProjects.length : projects.length}
              <span>/</span>
              {$allProjects.length}
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

      <FilterBar bind:search={$queryParams.projectSearch} filters={queryParams} defaultValues={defaultQueryParams} bind:hasActiveFilter={hasActiveProjectFilter}>
        <svelte:fragment slot="activeFilters" let:activeFilters>
          {#each activeFilters as filter}
              {#if filter.key === 'projectType'}
                <ActiveFilter {filter}>
                  <ProjectTypeIcon type={filter.value} />
                </ActiveFilter>
              {:else if filter.key === 'showDeletedProjects'}
                <ActiveFilter {filter}>
                  <TrashIcon color="text-error" />
                  {$t('admin_dashboard.project_filter.show_deleted')}
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
          <h2 class="card-title">Project filters</h2>
          <FormField label={$t('admin_dashboard.project_filter.project_member')}>
            {#if $queryParams.userEmail}
              <div class="join" use:bubbleFocusOnDestroy>
                <input class="input input-bordered join-item flex-grow" placeholder={$t('admin_dashboard.project_filter.all_users')} readonly value={$queryParams.userEmail} />
                  <div class="join-item isolate">
                    <IconButton
                      icon="i-mdi-close"
                      style="btn-outline"
                      on:click={() => $queryParams.userEmail = undefined}
                    />
                  </div>
              </div>
            {:else}
              <div class="alert alert-info gap-2">
                <span class="i-mdi-info-outline text-xl"></span>
                <div class="flex_ items-center gap-2">
                  <span class="mr-1">{$t('admin_dashboard.project_filter.select_user_from_table')}</span>
                  <span class="btn btn-sm btn-square pointer-events-none">
                    <span class="i-mdi-dots-vertical"></span>
                  </span>
                  <span class="i-mdi-chevron-right"></span>
                  <span class="btn btn-sm pointer-events-none normal-case font-normal">
                    <span class="i-mdi-filter-outline mr-1"></span>
                    {$t('admin_dashboard.filter_projects')}
                  </span>
                </div>
              </div>
            {/if}
          </FormField>
          <div class="form-control">
            <ProjectTypeSelect bind:value={$queryParams.projectType}
              undefinedOptionLabel={$t('project_type.any')} />
          </div>
          <div class="form-control">
            <label class="cursor-pointer label gap-4">
              <span class="label-text">{$t('admin_dashboard.show_delete_projects')}</span>
              <input
                bind:checked={$queryParams.showDeletedProjects}
                type="checkbox"
                class="toggle toggle-error"
              />
            </label>
          </div>
        </svelte:fragment>
      </FilterBar>

      <div class="divider" />
      <div class="overflow-x-auto">
        <table class="table table-lg">
          <thead>
            <tr class="bg-base-200">
              <th>{$t('admin_dashboard.column_name')}</th>
              <th>{$t('admin_dashboard.column_code')}</th>
              <th>{$t('admin_dashboard.column_users')}</th>
              <th>
                {$t('admin_dashboard.column_last_change')}
                <span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2" />
              </th>
              <th>{$t('admin_dashboard.column_type')}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {#each projects as project}
              <tr>
                <td>
                  {#if project.deletedDate}
                    <span class="flex gap-2 text-error items-center">
                      {project.name}
                      <TrashIcon pale />
                    </span>
                  {:else}
                    <a class="link" href={`/project/${project.code}`}>
                      {project.name}
                    </a>
                  {/if}
                </td>
                <td>{project.code}</td>
                <td>{project.userCount}</td>
                <td>
                  {#if project.deletedDate}
                    <span class="text-error">
                      <FormatDate date={project.deletedDate} />
                    </span>
                  {:else}
                    <FormatDate date={project.lastCommit} />
                  {/if}
                </td>
                <td>
                  <span class="tooltip align-bottom" data-tip={$t(getProjectTypeI18nKey(project.type))}>
                    <ProjectTypeIcon type={project.type} />
                  </span>
                </td>
                <td class="p-0">
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
              </tr>
            {/each}
          </tbody>
        </table>
        {#if hasActiveProjectFilter && projectFilterLimit < filteredProjects.length}
          <Button class="float-right mt-2" on:click={() => (projectFilterLimit = Infinity)}>
            {$t('admin_dashboard.load_more')}
          </Button>
        {/if}
      </div>
    </div>

    <div>
      <span class="text-xl flex gap-4">
        {$t('admin_dashboard.user_table_title')}
        <Badge>
          <span class="inline-flex gap-2">
            {userSearch ? filteredUsers.length : users.length}
            <span>/</span>
            {$allUsers.length}
          </span>
        </Badge>
      </span>
      <Input label="" placeholder={$t('admin_dashboard.filter_placeholder')} bind:value={userSearch} />

      <div class="divider" />
      <div class="overflow-x-auto">
        <table class="table table-lg">
          <thead>
            <tr class="bg-base-200">
              <th>
                {$t('admin_dashboard.column_name')}<span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2" />
              </th>
              <th>{$t('admin_dashboard.column_email')}</th>
              <th>{$t('admin_dashboard.column_role')}</th>
              <th>{$t('admin_dashboard.column_created')}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {#each users as user}
              <tr>
                <td>{user.name}</td>
                <td>
                  <span class="inline-flex items-center gap-2 text-left">
                    {user.email}
                    {#if !user.emailVerified}
                      <span
                        class="tooltip text-warning text-xl shrink-0 leading-0"
                        data-tip={$t('admin_dashboard.email_not_verified')}>
                        <span class="i-mdi-help-circle-outline" />
                      </span>
                    {/if}
                  </span>
                </td>
                <td class:text-accent={user.isAdmin}>
                  {user.isAdmin ? $t('user_types.admin') : $t('user_types.user')}
                </td>
                <td>
                  <FormatDate date={user.createdDate} />
                </td>
                <td class="p-0">
                  <Dropdown let:close>
                    <!-- svelte-ignore a11y-label-has-associated-control -->
                    <label tabindex="-1" class="btn btn-ghost btn-square">
                      <span class="i-mdi-dots-vertical text-lg" />
                    </label>
                    <ul slot="content" class="menu">
                      <li>
                        <button class="whitespace-nowrap" on:click={() => openModal(user)}>
                          <Icon icon="i-mdi-pencil-outline"  />
                          {$t('admin_dashboard.form_modal.title')}
                        </button>
                      </li>
                      <li>
                        <button class="whitespace-nowrap" on:click={() => {close();filterProjectsByUser(user);}}>
                          <Icon icon="i-mdi-filter-outline"  />
                          {$t('admin_dashboard.filter_projects')}
                        </button>
                      </li>
                    </ul>
                  </Dropdown>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
        {#if userSearch && userSearchLimit < filteredUsers.length}
          <Button class="float-right mt-2"
            on:click={() => (userSearchLimit = Infinity)}>
            {$t('admin_dashboard.load_more')}
          </Button>
        {/if}
      </div>
    </div>
  </div>

  <EditUserAccount bind:this={formModal} {deleteUser} currUser={data.user} />
  <DeleteUserModal bind:this={deleteUserModal} i18nScope="admin_dashboard.form_modal.delete_user" />
  <ConfirmDeleteModal bind:this={deleteProjectModal} i18nScope="delete_project_modal" />
</main>
