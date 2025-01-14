<script lang="ts">
  import { DetailsPage, DetailItem, AdminContent, PageBreadcrumb } from '$lib/layout';
  import t, { date } from '$lib/i18n';
  import { z } from 'zod';
  import EditableText from '$lib/components/EditableText.svelte';
  import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';
  import { Button, type ErrorMessage } from '$lib/forms';
  import type { PageData } from './$types';
  import { OrgRole } from '$lib/gql/types';
  import { useNotifications } from '$lib/notify';
  import { _changeOrgName, _deleteOrg, _orgMemberById, type OrgSearchParams, type User, type OrgUser, _removeProjectFromOrg, _leaveOrg } from './+page';
  import OrgTabs, { type OrgTabId } from './OrgTabs.svelte';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import { Icon, TrashIcon } from '$lib/icons';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
  import { goto } from '$app/navigation';
  import { DialogResponse } from '$lib/components/modals';
  import AddOrgMemberModal from './AddOrgMemberModal.svelte';
  import ChangeOrgMemberRoleModal from './ChangeOrgMemberRoleModal.svelte';
  import UserModal from '$lib/components/Users/UserModal.svelte';
  import OrgMemberTable from './OrgMemberTable.svelte';
  import ProjectTable from '$lib/components/Projects/ProjectTable.svelte';
  import type { UUID } from 'crypto';
  import BulkAddOrgMembers from './BulkAddOrgMembers.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import AddMyProjectsToOrgModal from './AddMyProjectsToOrgModal.svelte';
  import CreateUserModal from '$lib/components/Users/CreateUserModal.svelte';
  import {createGuestUserByAdmin, type LexAuthUser} from '$lib/user';
  import {Duration} from '$lib/util/time';
  import IconButton from '$lib/components/IconButton.svelte';

  export let data: PageData;
  $: user = data.user;
  let orgStore = data.org;
  $: org = $orgStore;

  const queryParams = getSearchParams<OrgSearchParams>({
    tab: queryParam.string<OrgTabId>('projects'),
  });
  const { queryParamValues } = queryParams;

  $: canManage = user.isAdmin || !!org.members.find(m => m.user.id === user.id && m.role === OrgRole.Admin)
  $: isMember = !!org.members.find(m => m.user.id === user.id)
  $: canSeeSettings = user.isAdmin || isMember

  const { notifySuccess, notifyWarning } = useNotifications();

  const orgNameValidation = z.string().trim().min(1, $t('org_page.org_name_empty_error'));

  async function updateOrgName(newName: string): Promise<ErrorMessage> {
    const result = await _changeOrgName({ orgId: org.id, name: newName });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('org_page.notifications.rename_org', { name: newName }));
  }

  let userModal: UserModal;
  async function openUserModal(user: User): Promise<void> {
    const queryUser = await _orgMemberById(org.id as UUID, user.id as UUID);
    return userModal.open(queryUser);
  }

  let addOrgMemberModal: AddOrgMemberModal;
  async function openAddOrgMemberModal(): Promise<void> {
    await addOrgMemberModal.openModal();
  }

  let bulkAddMembersModal: BulkAddOrgMembers;

  let changeMemberRoleModal: ChangeOrgMemberRoleModal;
  async function openChangeMemberRoleModal(member: OrgUser): Promise<void> {
    await changeMemberRoleModal.open({
      userId: member.user.id,
      name: member.user.name,
      role: member.role
    });
  }

  let deleteOrgModal: ConfirmDeleteModal;
  async function confirmDeleteOrg(): Promise<void> {
    const result = await deleteOrgModal.open(org.name, async () => {
      const { error } = await _deleteOrg(org.id);
      return error?.message;
    });
    if (result.response === DialogResponse.Submit) {
      notifyWarning($t('org_page.notifications.delete_org', { name: org.name }));
      await goto('/');
    }
  }

  let removeProjectFromOrgModal: DeleteModal;
  let projectToRemove: string;
  async function removeProjectFromOrg(projectId: string, projectName: string): Promise<void> {
    projectToRemove = projectName;
    const removed = await removeProjectFromOrgModal.prompt(async () => {
      const { error } = await _removeProjectFromOrg(projectId, org.id);
      return error?.message;
    });
    if (removed) {
      notifyWarning($t('org_page.notifications.remove_project_from_org', {projectName: projectToRemove}));
    }
  }

  let leaveModal: ConfirmModal;

  async function leaveOrg(): Promise<void> {
    const left = await leaveModal.open(async () => {
      const result = await _leaveOrg(org.id);
      if (result.error?.byType('LastMemberCantLeaveError')) {
        return $t('org_page.leave.last_to_leave');
      }
      return result.error?.message ? $t(`org_page.leave.error`) : undefined;
    });

    if (left) {
      notifySuccess($t(`org_page.leave.success`, { name: org.name }));
      await goto('/');
    }
  }

  let createUserModal: CreateUserModal;
  function onUserCreated(user: LexAuthUser): void {
    notifySuccess($t('admin_dashboard.notifications.user_created', { name: user.name }), Duration.Long);
  }
</script>

<PageBreadcrumb href="/org/list">{$t('org.table.title')}</PageBreadcrumb>

<DetailsPage wide titleText={org.name}>
  <svelte:fragment slot="actions">
    {#if isMember}
      <AddMyProjectsToOrgModal {user} {org} />
    {/if}
    {#if canManage}
    <div class="join gap-x-0.5">
      <Button variant="btn-success" class="join-item"
        on:click={openAddOrgMemberModal}>
        {$t('org_page.add_user.add_button')}
        <span class="i-mdi-account-plus-outline text-2xl" />
      </Button>
      <Dropdown>
        <IconButton icon="i-mdi-menu-down" variant="btn-success" join outline={false} />
        <ul slot="content" class="menu">
          <li>
            <button class="whitespace-nowrap text-success" on:click={() => bulkAddMembersModal.open()}>
              {$t('org_page.bulk_add_members.add_button')}
              <Icon icon="i-mdi-account-multiple-plus-outline" />
            </button>
          </li>
          <li>
            <button class="whitespace-nowrap text-success" on:click={() => createUserModal.open()}>
              {$t('admin_dashboard.create_user_modal.create_user')}
              <Icon icon="i-mdi-plus" />
            </button>
          </li>
        </ul>
      </Dropdown>
    </div>
    <CreateUserModal handleSubmit={createGuestUserByAdmin} on:submitted={(e) => onUserCreated(e.detail)} bind:this={createUserModal}/>
    <AddOrgMemberModal bind:this={addOrgMemberModal} {org} />
    <BulkAddOrgMembers bind:this={bulkAddMembersModal} orgId={org.id} />
    {/if}
  </svelte:fragment>
  <div slot="title" class="max-w-full flex items-baseline flex-wrap">
    <span class="mr-2">{$t('org_page.organization')}:</span>
    <span class="text-primary max-w-full">
      <EditableText
        disabled={!canManage}
        value={org.name}
        validation={orgNameValidation}
        saveHandler={updateOrgName}
      />
    </span>
  </div>
  <div class="mt-6">
    <OrgTabs bind:activeTab={$queryParamValues.tab} hideSettingsTab={!canSeeSettings} memberCount={org.members.length} projectCount={org.projects.length} />
  </div>
  <div class="py-6 px-2">
    {#if $queryParamValues.tab === 'projects'}
      <ProjectTable
        columns={['name', 'code', 'users', 'type']}
        projects={org.projects}
      >
        <td class="p-0" slot="actions" let:project>
          {#if canManage}
            <Dropdown>
              <button class="btn btn-ghost btn-square">
                <span class="i-mdi-dots-vertical text-lg" />
              </button>
              <ul slot="content" class="menu">
                <li>
                  <button class="text-error" on:click={() => removeProjectFromOrg(project.id, project.name)}>
                    <TrashIcon />
                    {$t('org_page.remove_project_from_org')}
                  </button>
                </li>
              </ul>
            </Dropdown>
          {/if}
        </td>
      </ProjectTable>
      <DeleteModal
        bind:this={removeProjectFromOrgModal}
        entityName={$t('org_page.remove_project_from_org_title')}
        isRemoveDialog
      >
        {$t('org_page.confirm_remove_project_from_org', {projectName: projectToRemove, orgName: org.name})}
      </DeleteModal>
    {:else if $queryParamValues.tab === 'members'}
      <OrgMemberTable
        {org}
        {user}
        shownUsers={org.members}
        {canManage}
        on:openUserModal={(event) => openUserModal(event.detail)}
        on:changeMemberRole={(event) => openChangeMemberRoleModal(event.detail)}
      />
    {:else if $queryParamValues.tab === 'history'}
      <div class="space-y-2">
        <DetailItem title={$t('org_page.details.created_at')} text={$date(org.createdDate)} />
        <DetailItem title={$t('org_page.details.updated_at')} text={$date(org.updatedDate)} />
      </div>
    {:else if $queryParamValues.tab === 'settings'}
      {#if isMember}
        <div class="flex justify-end">
          <Button outline variant="btn-error" on:click={leaveOrg}>
            {$t('org_page.leave.leave_org')}
            <Icon icon="i-mdi-exit-run"/>
          </Button>
          <ConfirmModal bind:this={leaveModal}
                        title={$t('org_page.leave.confirm_title')}
                        submitText={$t('org_page.leave.leave_action')}
                        submitIcon="i-mdi-exit-run"
                        submitVariant="btn-error"
                        cancelText={$t('org_page.leave.dont_leave')}>
            <p>{$t('org_page.leave.confirm_leave')}</p>
          </ConfirmModal>
        </div>
      {/if}
      <AdminContent>
        <div class="divider" />
        <div class="flex justify-end">
          <Button variant="btn-error" on:click={confirmDeleteOrg}>
            {$t('org_page.delete_modal.submit')}
            <TrashIcon/>
          </Button>
        </div>
      </AdminContent>
    {/if}
  </div>
</DetailsPage>
<ConfirmDeleteModal bind:this={deleteOrgModal} i18nScope="delete_org_modal" />
<ChangeOrgMemberRoleModal orgId={org.id} bind:this={changeMemberRoleModal} />
<UserModal bind:this={userModal} />
