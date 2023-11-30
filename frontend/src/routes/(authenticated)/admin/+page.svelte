<script lang="ts">
  import { Badge } from '$lib/components/Badges';
  import Input from '$lib/forms/Input.svelte';
  import t from '$lib/i18n';
  import type { PageData } from './$types';
  import DeleteUserModal from '$lib/components/DeleteUserModal.svelte';
  import EditUserAccount from './EditUserAccount.svelte';
  import { useNotifications } from '$lib/notify';
  import { DialogResponse } from '$lib/components/modals';
  import { Duration } from '$lib/util/time';
  import { Icon } from '$lib/icons';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { RefineFilterMessage } from '$lib/components/Table';
  import type { AdminSearchParams, User } from './+page';
  import ProjectTable from './ProjectTable.svelte';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import type { ProjectType, ProjectMigrationStatus } from '$lib/gql/types';

  export let data: PageData;
  $: allProjects = data.projects;
  $: userData = data.users;

  const { notifySuccess, notifyWarning } = useNotifications();

  const queryParams = getSearchParams<AdminSearchParams>({
    userSearch: queryParam.string<string>(''),
    showDeletedProjects: queryParam.boolean<boolean>(false),
    projectType: queryParam.string<ProjectType | undefined>(undefined),
    userEmail: queryParam.string(undefined),
    projectSearch: queryParam.string<string>(''),
    migrationStatus: queryParam.string<ProjectMigrationStatus | 'UNMIGRATED' | undefined>(undefined),
  });

  const { queryParamValues } = queryParams;

  $: users = $userData?.items ?? [];
  $: totalUsers = $userData?.totalCount ?? 0;
  $: shownUsers = $queryParamValues.userSearch ? users : users.slice(0, 10);

  function filterProjectsByUser(user: User): void {
    $queryParamValues.userEmail = user.email;
  }

  let projectsTable: ProjectTable;
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


</script>

<svelte:head>
  <title>{$t('admin_dashboard.title')}</title>
</svelte:head>
<main>
  <div class="grid lg:grid-cols-2 grid-cols-1 gap-10">
    <ProjectTable bind:this={projectsTable} {queryParams} projects={$allProjects} />

    <div>
      <span class="text-xl flex gap-4">
        {$t('admin_dashboard.user_table_title')}
        <Badge>
          <span class="inline-flex gap-2">
            {shownUsers.length}
            <span>/</span>
            {totalUsers}
          </span>
        </Badge>
      </span>
      <Input label="" placeholder={$t('admin_dashboard.filter_placeholder')} bind:value={$queryParamValues.userSearch} debounce />

      <div class="divider" />
      <div class="overflow-x-auto min-h-[300px]">
        <table class="table table-lg">
          <thead>
            <tr class="bg-base-200">
              <th>
                {$t('admin_dashboard.column_name')}<span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2" />
              </th>
              <th>{$t('admin_dashboard.column_login')}</th>
              <th>{$t('admin_dashboard.column_email')}</th>
              <th>{$t('admin_dashboard.column_role')}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {#each shownUsers as user}
              <tr>
                <td>{user.name}</td>
                <td>
                  {#if user.username}
                    {user.username}
                  {/if}
                </td>
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
        <RefineFilterMessage total={totalUsers} showing={shownUsers.length} />
      </div>
    </div>
  </div>

  <EditUserAccount bind:this={formModal} {deleteUser} currUser={data.user} />
  <DeleteUserModal bind:this={deleteUserModal} i18nScope="admin_dashboard.form_modal.delete_user" />
</main>
