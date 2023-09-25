<script lang="ts">
  import { Badge } from '$lib/components/Badges';
  import FormatDate from '$lib/components/FormatDate.svelte';
  import Input from '$lib/forms/Input.svelte';
  import t from '$lib/i18n';
  import type { PageData } from './$types';
  import DeleteUserModal from '$lib/components/DeleteUserModal.svelte';
  import EditUserAccount from './EditUserAccount.svelte';
  import { notifySuccess, notifyWarning } from '$lib/notify';
  import { DialogResponse } from '$lib/components/modals';
  import { Duration } from '$lib/util/time';
  import { Icon } from '$lib/icons';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import Button from '$lib/forms/Button.svelte';
  import type { User } from './+page';
  import ProjectTable from './ProjectTable.svelte';


  export let data: PageData;
  $: allProjects = data.projects;
  $: allUsers = data.users;

  let projectsTable: ProjectTable;
  function filterProjectsByUser(user: User): void {
    projectsTable.setUserFilter(user.email);
  }

  let deleteUserModal: DeleteUserModal;
  let formModal: EditUserAccount;

  async function deleteUser(user: User): Promise<void> {
    formModal.close();
    const { response } = await deleteUserModal.open(user);
    if (response == DialogResponse.Submit) {
      notifyWarning($t('admin_dashboard.notifications.user_deleted', { name: user.name }));
    }
  }

  async function openModal(user: User): Promise<void> {
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
  let userSearch = '';

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

</script>

<svelte:head>
  <title>{$t('admin_dashboard.title')}</title>
</svelte:head>
<main class="flex justify-center">
  <div class="grid lg:grid-cols-2 grid-cols-1 gap-10">
    <ProjectTable bind:this={projectsTable} projects={$allProjects} users={$allUsers} {defaultFilterLimit}/>

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
</main>
