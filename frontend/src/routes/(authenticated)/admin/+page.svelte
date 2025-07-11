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
  import { type AdminSearchParams, type User } from './+page';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import type { ProjectType } from '$lib/gql/types';
  import { derived as derivedStore } from 'svelte/store';
  import AdminProjects from './AdminProjects.svelte';
  import UserModal from '$lib/components/Users/UserModal.svelte';
  import { PageBreadcrumb } from '$lib/layout';
  import AdminTabs, { type AdminTabId } from './AdminTabs.svelte';
  import { createGuestUserByAdmin, type LexAuthUser } from '$lib/user';
  import CreateUserModal from '$lib/components/Users/CreateUserModal.svelte';
  import type { Confidentiality } from '$lib/components/Projects';
  import { browser } from '$app/environment';
  import UserTable from '$lib/components/Users/UserTable.svelte';
  import UserFilter, { type UserFilters, type UserType } from '$lib/components/Users/UserFilter.svelte';
  import { untrack } from 'svelte';

  interface Props {
    data: PageData;
  }

  const { data }: Props = $props();
  let projects = $derived(data.projects);
  let draftProjects = $derived(data.draftProjects);
  let userData = $derived(data.users);

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
    userType: queryParam.string<UserType>(undefined),
    tab: queryParam.string<AdminTabId>('projects'),
  });

  const userFilterKeys = ['userSearch', 'usersICreated', 'userType'] as const satisfies Readonly<(keyof UserFilters)[]>;

  const { queryParamValues, defaultQueryParamValues } = queryParams;
  let tab = $derived($queryParamValues.tab);

  const loadingUsers = derivedStore(navigating, (nav) => {
    if (!nav?.to?.route.id?.endsWith('/admin')) return false;
    const fromUrl = nav?.from?.url;
    return (
      fromUrl &&
      userFilterKeys.some(
        (key) => (fromUrl.searchParams.get(key) ?? defaultQueryParamValues[key])?.toString() !== $queryParamValues[key],
      )
    );
  });

  let hasActiveFilter = $state(false);
  let lastLoadUsedActiveFilter = $state(false);
  // Not using $derived here because we want lastLoadUsedActiveFilter to only change value when $loadingUsers becomes false
  $effect(() => {
    if (!$loadingUsers) {
      lastLoadUsedActiveFilter = untrack(() => hasActiveFilter);
    }
  });
  let users = $derived($userData?.items ?? []);
  let filteredUserCount = $derived($userData?.totalCount ?? 0);
  let shownUsers = $derived(lastLoadUsedActiveFilter ? users : users.slice(0, 10));

  function filterProjectsByUser(user: User): void {
    $queryParamValues.memberSearch = user.email ?? user.username ?? undefined;
    // Clear other filters that might hide the user's projects
    $queryParamValues.projectSearch = '';
    $queryParamValues.projectType = undefined;
    $queryParamValues.tab = 'projects';
  }

  let userModal: UserModal | undefined = $state();
  let createUserModal: CreateUserModal | undefined = $state();
  let deleteUserModal: DeleteUserModal | undefined = $state();
  let formModal: EditUserAccount | undefined = $state();

  async function deleteUser(user: User): Promise<void> {
    if (!formModal || !deleteUserModal) return;
    formModal.close();
    const { response } = await deleteUserModal.open(user);
    if (response == DialogResponse.Submit) {
      notifyWarning($t('admin_dashboard.notifications.user_deleted', { name: user.name }));
    }
  }

  async function openModal(user: User): Promise<void> {
    if (!formModal) return;
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
      <AdminTabs activeTab="users" onClickTab={(tab) => ($queryParamValues.tab = tab)}>
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
          <!-- svelte-ignore a11y_no_static_element_interactions -->
          <svelte:element
            this={browser ? 'button' : 'div'}
            class="btn btn-sm btn-success max-xs:btn-square"
            onclick={() => createUserModal?.open()}
          >
            <span class="admin-tabs:hidden">
              {$t('admin_dashboard.create_user_modal.create_user')}
            </span>
            <span class="i-mdi-plus text-2xl"></span>
          </svelte:element>
        </div>
      </AdminTabs>
      <div class="mt-4">
        <UserFilter
          filters={queryParamValues}
          filterDefaults={defaultQueryParamValues}
          filterKeys={userFilterKeys}
          bind:hasActiveFilter
          loading={$loadingUsers}
        />
      </div>

      <div class="divider"></div>
      <div class="overflow-x-visible @container scroll-shadow">
        <UserTable
          {shownUsers}
          onOpenUserModal={(user) => userModal?.open(user)}
          onEditUser={openModal}
          onFilterProjectsByUser={filterProjectsByUser}
        />
        <RefineFilterMessage total={filteredUserCount} showing={shownUsers.length} />
      </div>
    </div>
  </div>

  <EditUserAccount bind:this={formModal} {deleteUser} currUser={data.user} />
  <DeleteUserModal bind:this={deleteUserModal} i18nScope="admin_dashboard.form_modal.delete_user" />
  <UserModal bind:this={userModal} />
  <CreateUserModal handleSubmit={createGuestUserByAdmin} onSubmitted={onUserCreated} bind:this={createUserModal} />
</main>
