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
  import { Duration, notifySuccess, notifyWarning } from '$lib/notify';
  import { DialogResponse } from '$lib/components/modals';

  type UserRow = LoadAdminDashboardQuery['users'][0];

  export let data: PageData;
  $: allProjects = data.projects;
  $: allUsers = data.users;
  let deleteModal: DeleteUserModal;
  let formModal: EditUserAccount;
  let _editing: UserRow;
  async function deleteUser(id: string): Promise<void> {
    formModal.close();
    const { response } = await deleteModal.open(id);
    if (response == DialogResponse.Submit) {
      notifyWarning($t('admin_dashboard.notifications.user_deleted', { name: _editing.name }));
    }
  }

  async function openModal(user: UserRow): Promise<void> {
    _editing = user;
    const { response, formState } = await formModal.openModal(user);
    if (response == DialogResponse.Submit) {
      if (formState.name.tainted || formState.password.tainted) {
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

  let projectSearch = '';
  let userSearch = '';
  $: projectSearchLower = projectSearch.toLocaleLowerCase();
  $: projects = $allProjects
    .filter(
      (p) =>
        !projectSearch ||
        p.name.toLocaleLowerCase().includes(projectSearchLower) ||
        p.code.toLocaleLowerCase().includes(projectSearchLower)
    )
    .slice(0, projectSearch ? undefined : 10);
  $: userSearchLower = userSearch.toLocaleLowerCase();
  $: users = $allUsers
    .filter(
      (u) =>
        !userSearch ||
        u.name.toLocaleLowerCase().includes(userSearchLower) ||
        u.email.toLocaleLowerCase().includes(userSearchLower)
    )
    .slice(0, userSearch ? undefined : 10);
</script>

<svelte:head>
  <title>{$t('admin_dashboard.title')}</title>
</svelte:head>

<main>
  <div class="grid grid-cols-2 m:grid-cols-1">
    <div class="pl-1 overflow-x-auto">
      <span class="text-xl">
        {$t('admin_dashboard.project_table_title')}
        <Badge>{projectSearch ? projects.length : $allProjects.length}</Badge>
      </span>

      <Input
        type="text"
        label=""
        placeholder={$t('admin_dashboard.filter_placeholder')}
        autofocus
        bind:value={projectSearch}
      />

      <div class="divider" />

      <table class="table">
        <thead>
          <tr>
            <th>{$t('admin_dashboard.column_name')}</th>
            <th>{$t('admin_dashboard.column_code')}</th>
            <th>{$t('admin_dashboard.column_users')}</th>
            <th
              >{$t('admin_dashboard.column_last_change')}<span
                class="i-mdi-sort-ascending text-xl align-[-5px] ml-1"
              /></th
            >
            <th>{$t('admin_dashboard.column_type')}</th>
          </tr>
        </thead>
        <tbody>
          {#each projects as project}
            <tr>
              <td>
                <a class="link" href={`/project/${project.code}`}>
                  {project.name}
                </a>
              </td>
              <td>{project.code}</td>
              <td>TODO</td>
              <td>
                <FormatDate date={project.lastCommit} />
              </td>
              <td>
                <FormatProjectType type={project.type} />
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>

    <div class="pl-1 overflow-x-auto">
      <span class="text-xl">
        {$t('admin_dashboard.user_table_title')}
        <Badge>{userSearch ? users.length : $allUsers.length}</Badge>
      </span>
      <Input label="" placeholder={$t('admin_dashboard.filter_placeholder')} bind:value={userSearch} />

      <div class="divider" />
      <table class="table">
        <thead>
          <tr>
            <th>{$t('admin_dashboard.column_name')}<span class="i-mdi-sort-ascending text-xl align-[-5px] ml-1" /></th>
            <th>{$t('admin_dashboard.column_email')}</th>
            <th>{$t('admin_dashboard.column_role')}</th>
            <th>{$t('admin_dashboard.column_created')}</th>
            <th>{$t('admin_dashboard.column_edit')}</th>
          </tr>
        </thead>
        <tbody>
          {#each users as user}
            <tr>
              <td>{user.name}</td>
              <td>{user.email}</td>
              <td>{user.isAdmin ? $t('user_types.admin') : $t('user_types.user')}</td>
              <td>
                <FormatDate date={user.createdDate} />
              </td>
              <td class="p-0">
                <IconButton ghost icon="i-mdi-pencil-outline" on:click={() => openModal(user)} />
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  </div>

  <EditUserAccount bind:this={formModal} {deleteUser} />
  <DeleteUserModal bind:this={deleteModal} />
</main>
