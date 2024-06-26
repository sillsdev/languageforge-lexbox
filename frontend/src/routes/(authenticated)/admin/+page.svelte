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
  import { RefineFilterMessage } from '$lib/components/Table';
  import type { AdminSearchParams, User } from './+page';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import type { ProjectType } from '$lib/gql/types';
  import { derived } from 'svelte/store';
  import AdminProjects from './AdminProjects.svelte';
  import UserModal from '$lib/components/Users/UserModal.svelte';
  import { PageBreadcrumb } from '$lib/layout';
  import AdminTabs, { type AdminTabId } from './AdminTabs.svelte';
  import { createGuestUserByAdmin, type LexAuthUser } from '$lib/user';
  import CreateUserModal from '$lib/components/Users/CreateUserModal.svelte';
  import type { Confidentiality } from '$lib/components/Projects';
  import { browser } from '$app/environment';
  import UserTable from './UserTable.svelte';
  import UserFilter, { filterUsers } from './UserFilter.svelte';

  export let data: PageData;
  $: projects = data.projects;
  $: draftProjects = data.draftProjects;
  $: userData = data.users;
  $: adminId = data.user?.id;

  const { notifySuccess, notifyWarning } = useNotifications();

  const queryParams = getSearchParams<AdminSearchParams>({
    userSearch: queryParam.string<string>(''),
    showDeletedProjects: queryParam.boolean<boolean>(false),
    hideDraftProjects: queryParam.boolean<boolean>(false),
    emptyProjects: queryParam.boolean<boolean>(false),
    // eslint-disable-next-line @typescript-eslint/no-redundant-type-constituents -- false positive?
    confidential: queryParam.string<Confidentiality | undefined>(undefined),
    projectType: queryParam.string<ProjectType | undefined>(undefined),
    memberSearch: queryParam.string(undefined),
    projectSearch: queryParam.string<string>(''),
    usersICreated: queryParam.boolean<boolean>(false),
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
  $: filters = queryParams.queryParamValues;
  $: filteredUsers = filterUsers(users, $filters, adminId);
  $: shownUsers = lastLoadUsedActiveFilter ? filteredUsers : filteredUsers.slice(0, 10);

  function filterProjectsByUser(user: User): void {
    $queryParamValues.memberSearch = user.email ?? user.username ?? undefined;
    // Clear other filters that might hide the user's projects
    $queryParamValues.projectSearch = '';
    $queryParamValues.projectType = undefined;
    $queryParamValues.tab = 'projects';
  }

  let userModal: UserModal;
  let createUserModal: CreateUserModal;
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
      if (formState.email.changed && formState.email.currentValue) {
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

  function onUserCreated(user: LexAuthUser): void {
    notifySuccess($t('admin_dashboard.notifications.user_created', { name: user.name }), Duration.Long);
    $queryParamValues.userSearch = user.emailOrUsername;
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
        <div class="flex gap-4 justify-between grow">
          <div class="flex gap-4 items-center">
            {$t('admin_dashboard.user_table_title')}
            <div class="contents max-xs:hidden">
              <Badge>
                <span class="inline-flex gap-2">
                  {$number(shownUsers.length)}
                  <span>/</span>
                  {$number(filteredUserCount)}
                </span>
              </Badge>
            </div>
          </div>
          <!-- svelte-ignore a11y-no-static-element-interactions -->
          <svelte:element this={browser ? 'button' : 'div'} class="btn btn-sm btn-success max-xs:btn-square"
            on:click={() => createUserModal.open()}>
            <span class="admin-tabs:hidden">
              {$t('admin_dashboard.create_user_modal.create_user')}
            </span>
            <span class="i-mdi-plus text-2xl" />
          </svelte:element>
        </div>
      </AdminTabs>
      <div class="mt-4">
        <UserFilter
          filters={queryParamValues}
          filterDefaults={defaultQueryParamValues}
          bind:hasActiveFilter
          loading={$loadingUsers}
        />
      </div>

      <div class="divider" />
      <div class="overflow-x-auto @container scroll-shadow">
        <UserTable
          {shownUsers}
          on:openUserModal={(event) => userModal.open(event.detail)}
          on:editUser={(event) => openModal(event.detail)}
          on:filterProjectsByUser={(event) => filterProjectsByUser(event.detail)}
        />
        <RefineFilterMessage total={filteredUserCount} showing={shownUsers.length} />
      </div>
    </div>
  </div>

  <EditUserAccount bind:this={formModal} {deleteUser} currUser={data.user} />
  <DeleteUserModal bind:this={deleteUserModal} i18nScope="admin_dashboard.form_modal.delete_user" />
  <UserModal bind:this={userModal}/>
  <CreateUserModal handleSubmit={createGuestUserByAdmin} on:submitted={(e) => onUserCreated(e.detail)} bind:this={createUserModal}/>
</main>
