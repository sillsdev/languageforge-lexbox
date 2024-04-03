<script lang="ts">
  import { navigating } from '$app/stores';
  import { Badge } from '$lib/components/Badges';
  import t, { number } from '$lib/i18n';
  import type { PageData } from './$types';
  import DeleteUserModal from '$lib/components/DeleteUserModal.svelte';
  import EditUserAccount from './EditUserAccount.svelte';
  import { useNotifications } from '$lib/notify';
  import { DialogResponse } from '$lib/components/modals';
  import { Duration } from '$lib/util/time';
  import { AdminIcon, Icon } from '$lib/icons';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import FilterBar from '$lib/components/FilterBar/FilterBar.svelte';
  import { RefineFilterMessage } from '$lib/components/Table';
  import type { AdminSearchParams, User } from './+page';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import type { ProjectType } from '$lib/gql/types';
  import { derived } from 'svelte/store';
  import AdminProjects from './AdminProjects.svelte';
  import UserModal from '$lib/components/Users/UserModal.svelte';
  import { Button } from '$lib/forms';
  import { PageBreadcrumb } from '$lib/layout';
  import AdminTabs, { type AdminTabId } from './AdminTabs.svelte';

  export let data: PageData;
  $: projects = data.projects;
  $: draftProjects = data.draftProjects;
  $: userData = data.users;

  const { notifySuccess, notifyWarning } = useNotifications();

  const queryParams = getSearchParams<AdminSearchParams>({
    userSearch: queryParam.string<string>(''),
    showDeletedProjects: queryParam.boolean<boolean>(false),
    hideDraftProjects: queryParam.boolean<boolean>(false),
    projectType: queryParam.string<ProjectType | undefined>(undefined),
    userEmail: queryParam.string(undefined),
    projectSearch: queryParam.string<string>(''),
    tab: queryParam.string<AdminTabId>('projects'),
  });

  const userFilterKeys = ['userSearch'] as const satisfies Readonly<(keyof AdminSearchParams)[]>;
  const { queryParamValues, defaultQueryParamValues } = queryParams;
  $: tab = $queryParamValues.tab;

  const loadingUsers = derived(navigating, (nav) => {
    const fromUrl = nav?.from?.url;
    return fromUrl && userFilterKeys.some((key) =>
      (fromUrl.searchParams.get(key) ?? defaultQueryParamValues[key])?.toString() !== $queryParamValues[key]);
  });

  let hasActiveFilter = false;
  let lastLoadUsedActiveFilter = false;
  $: if (!$loadingUsers) lastLoadUsedActiveFilter = hasActiveFilter;

  $: users = $userData?.items ?? [];
  $: filteredUserCount = $userData?.totalCount ?? 0;
  $: shownUsers = lastLoadUsedActiveFilter ? users : users.slice(0, 10);

  function filterProjectsByUser(user: User): void {
    $queryParamValues.userEmail = user.email ?? user.username ?? undefined;
  }

  let userModal: UserModal;
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
          Duration.Long,
        );
      }
    }
  }
</script>

<svelte:head>
  <title>{$t('admin_dashboard.title')}</title>
</svelte:head>
<PageBreadcrumb>{$t('admin_dashboard.title')}</PageBreadcrumb>
<main>
  <div class="grid grid-cols-2 admin-tabs:grid-cols-1 gap-10">
    <div class="contents" class:admin-tabs:hidden={tab === 'users'}>
    <AdminProjects projects={$projects} draftProjects={$draftProjects} {queryParams} />
    </div>

    <div class:admin-tabs:hidden={tab !== 'users'}>
      <AdminTabs activeTab="users" on:clickTab={(event) => $queryParamValues.tab = event.detail}>
        {$t('admin_dashboard.user_table_title')}
        <Badge>
          <span class="inline-flex gap-2">
            {$number(shownUsers.length)}
            <span>/</span>
            {$number(filteredUserCount)}
          </span>
        </Badge>
      </AdminTabs>
      <div class="mt-4">
        <FilterBar
          debounce
          loading={$loadingUsers}
          searchKey="userSearch"
          filterKeys={userFilterKeys}
          filters={queryParamValues}
          filterDefaults={defaultQueryParamValues}
          bind:hasActiveFilter
        />
      </div>

      <div class="divider" />
      <div class="overflow-x-auto @container">
        <table class="table table-lg">
          <thead>
            <tr class="bg-base-200">
              <th>
                {$t('admin_dashboard.column_name')}<span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2" />
              </th>
              <th class="hidden @xl:table-cell">{$t('admin_dashboard.column_login')}</th>
              <th>{$t('admin_dashboard.column_email')}</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {#each shownUsers as user}
              <tr>
                <td>
                  <div class="flex items-center gap-2">
                    <Button variant="btn-ghost" size="btn-sm" on:click={() => userModal.open(user)}>
                      {user.name}
                      <Icon icon="i-mdi-card-account-details-outline" />
                    </Button>
                    {#if user.locked}
                      <span
                          class="tooltip text-warning text-xl leading-0"
                          data-tip={$t('admin_dashboard.user_is_locked')}>
                        <Icon icon="i-mdi-lock" />
                      </span>
                    {/if}
                    {#if user.isAdmin}
                      <span
                          class="tooltip text-accent text-xl leading-0"
                          data-tip={$t('user_types.admin')}>
                          <AdminIcon size="text-xl" />
                      </span>
                    {/if}
                  </div>
                </td>
                <td class="hidden @xl:table-cell">
                  {#if user.username}
                    {user.username}
                  {/if}
                </td>
                <td>
                  <span class="inline-flex items-center gap-2 text-left">
                    {#if user.email}
                      {user.email}
                      {#if !user.emailVerified}
                        <span
                          class="tooltip text-warning text-xl shrink-0 leading-0"
                          data-tip={$t('admin_dashboard.email_not_verified')}>
                          <span class="i-mdi-help-circle-outline" />
                        </span>
                      {/if}
                    {:else}
                      –
                    {/if}
                  </span>
                </td>
                <td class="p-0">
                  <Dropdown>
                    <button class="btn btn-ghost btn-square">
                      <span class="i-mdi-dots-vertical text-lg" />
                    </button>
                    <ul slot="content" class="menu">
                      <li>
                        <button class="whitespace-nowrap" on:click={() => openModal(user)}>
                          <Icon icon="i-mdi-pencil-outline" />
                          {$t('admin_dashboard.form_modal.title')}
                        </button>
                      </li>
                      <li>
                        <button class="whitespace-nowrap" on:click={() => filterProjectsByUser(user)}>
                          <Icon icon="i-mdi-filter-outline" />
                          {$t('project.filter.filter_user_projects')}
                        </button>
                      </li>
                    </ul>
                  </Dropdown>
                </td>
              </tr>
            {/each}
          </tbody>
        </table>
        <RefineFilterMessage total={filteredUserCount} showing={shownUsers.length} />
      </div>
    </div>
  </div>

  <EditUserAccount bind:this={formModal} {deleteUser} currUser={data.user} />
  <DeleteUserModal bind:this={deleteUserModal} i18nScope="admin_dashboard.form_modal.delete_user" />
  <UserModal bind:this={userModal}/>
</main>
