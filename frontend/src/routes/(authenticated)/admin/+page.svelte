<script lang="ts">
  import { Badge } from '$lib/components/Badges';
  import FormatDate from '$lib/components/FormatDate.svelte';
  import FormatProjectType from '$lib/components/FormatProjectType.svelte';
  import Input from '$lib/forms/Input.svelte';
  import t from '$lib/i18n';
  import type { PageData } from './$types';
  import IconButton from '$lib/components/IconButton.svelte';
  import DeleteUserModal from '$lib/components/DeleteUserModal.svelte';
  import EditUserAccount from './EditUserAccount.svelte';
  import type { LoadAdminDashboardQuery } from '$lib/gql/types';
  import { notifySuccess, notifyWarning } from '$lib/notify';
  import { DialogResponse } from '$lib/components/modals';
  import { Duration } from '$lib/util/time';
  import { TrashIcon } from '$lib/icons';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import type { AdminSearchParams } from './+page';
  import { goto } from '$app/navigation';
  import { getBoolSearchParam, toSearchParams } from '$lib/util/urls';
  import { page } from '$app/stores';
  import { _deleteProject } from '$lib/gql/mutations';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import Button from '$lib/forms/Button.svelte';

  type UserRow = LoadAdminDashboardQuery['users'][0];

  export let data: PageData;
  $: allProjects = data.projects;
  $: allUsers = data.users;

  const options: AdminSearchParams = {
    showDeletedProjects: getBoolSearchParam<AdminSearchParams>('showDeletedProjects', $page.url.searchParams),
  };

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

  const defaultSearchLimit = 100;

  let projectSearch = '';
  let userSearch = '';
  $: projectSearchLower = projectSearch.toLocaleLowerCase();
  let projectSearchLimit = defaultSearchLimit;
  $: projectLimit = projectSearch ? projectSearchLimit : 10;
  $: filteredProjects = $allProjects
    .filter(
      (p) =>
        !projectSearch ||
        p.name.toLocaleLowerCase().includes(projectSearchLower) ||
        p.code.toLocaleLowerCase().includes(projectSearchLower)
    );
  $: projects = filteredProjects.slice(0, projectLimit);
  $: {
    // Reset limit if search is changed
    projectSearch;
    projectSearchLimit = defaultSearchLimit;
  }

  $: userSearchLower = userSearch.toLocaleLowerCase();
  let userSearchLimit = defaultSearchLimit;
  $: userLimit = userSearch ? userSearchLimit : 10;
  $: filteredUsers = $allUsers
    .filter(
      (u) =>
        !userSearch ||
        u.name.toLocaleLowerCase().includes(userSearchLower) ||
        u.email.toLocaleLowerCase().includes(userSearchLower)
    );
  $: users = filteredUsers.slice(0, userLimit);
  $: {
    // Reset limit if search is changed
    userSearch;
    userSearchLimit = defaultSearchLimit;
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

  async function refreshPage(): Promise<void> {
    await goto(`?${toSearchParams(options)}`, {
      replaceState: true,
      noScroll: true,
      keepFocus: true,
    });
  }
</script>

<svelte:head>
  <title>{$t('admin_dashboard.title')}</title>
</svelte:head>
<main class="flex justify-center">
  <div class="grid lg:grid-cols-2 grid-cols-1 gap-10">
    <div>
      <span class="text-xl flex gap-4">
        {$t('admin_dashboard.project_table_title')}
        <Badge>
          <span class="inline-flex gap-2">
            {projectSearch ? filteredProjects.length : projects.length}
            <span>/</span>
            {$allProjects.length}
          </span>
        </Badge>
      </span>

      <div class="flex items-end gap-4">
        <Input
          type="text"
          label=""
          placeholder={$t('admin_dashboard.filter_placeholder')}
          autofocus
          bind:value={projectSearch}
        />
        <Dropdown>
          <!-- svelte-ignore a11y-label-has-associated-control -->
          <label tabindex="-1" class="btn flex flex-nowrap gap-2" class:text-success={options.showDeletedProjects}>
            {$t('admin_dashboard.options')}
            <span class="i-mdi-dots-vertical text-xl" />
          </label>
          <ul slot="content" class="menu">
            <li>
              <div class="whitespace-nowrap">
                <label class="cursor-pointer label gap-4">
                  <span class="label-text">{$t('admin_dashboard.show_delete_projects')}</span>
                  <input
                    bind:checked={options.showDeletedProjects}
                    type="checkbox"
                    class="toggle toggle-error"
                    on:change={refreshPage}
                  />
                </label>
              </div>
            </li>
          </ul>
        </Dropdown>
      </div>

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
                  <FormatProjectType type={project.type} />
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
        {#if projectSearch && projectSearchLimit < filteredProjects.length}
            <Button class="float-right mt-2" on:click={() => projectSearchLimit = Infinity}>Load all</Button>
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
                <td class:tooltip={!user.emailVerified} data-tip={$t('admin_dashboard.email_not_verified')}>
                  {user.email}
                  {#if !user.emailVerified}
                    <span class="tooltip i-mdi-question-mark text-warning mb-[-3px]"/>
                  {/if}
                </td>
                <td class:text-accent={user.isAdmin}>
                  {user.isAdmin ? $t('user_types.admin') : $t('user_types.user')}
                </td>
                <td>
                  <FormatDate date={user.createdDate} />
                </td>
                <td class="p-0">
                  <IconButton icon="i-mdi-pencil-outline" style="btn-ghost" on:click={() => openModal(user)} />
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
        {#if userSearch && userSearchLimit < filteredUsers.length}
          <Button class="float-right mt-2" on:click={() => userSearchLimit = Infinity}>Load all</Button>
        {/if}
      </div>
    </div>
  </div>

  <EditUserAccount bind:this={formModal} {deleteUser} currUser={data.user} />
  <DeleteUserModal bind:this={deleteUserModal} i18nScope="admin_dashboard.form_modal.delete_user" />
  <ConfirmDeleteModal bind:this={deleteProjectModal} i18nScope="delete_project_modal" />
</main>
