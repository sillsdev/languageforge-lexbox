<script lang="ts">
  import { DetailsPage, DetailItem, AdminContent } from '$lib/layout';
  import t, { date } from '$lib/i18n';
  import { z } from 'zod';
  import EditableText from '$lib/components/EditableText.svelte';
  import { Button, type ErrorMessage } from '$lib/forms';
  import type { PageData } from './$types';
  import { OrgRole } from '$lib/gql/types';
  import { useNotifications } from '$lib/notify';
  import { _changeOrgName, _deleteOrgUser, _deleteOrg, _orgMemberById, type OrgSearchParams, type User, type OrgUser, _removeProjectFromOrg } from './+page';
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
      notifyWarning('You have successfully removed project from org');
    }
  }

  async function leaveOrg(): Promise<void> {
    const result = await _deleteOrgUser(org.id, user.id);
    if (result.error) {
      notifyWarning($t(`org_page.notifications.leave_org_error`, { name: org.name }));
    } else {
      notifySuccess($t(`org_page.notifications.leave_org`, { name: org.name }));
      await goto('/');
    }
  }
</script>

<DetailsPage wide title={org.name}>
  <svelte:fragment slot="actions">
    {#if canManage}
      <Button variant="btn-success"
        on:click={openAddOrgMemberModal}>
        <span class="admin-tabs:hidden">
          {$t('org_page.add_user.add_button')}
        </span>
        <span class="i-mdi-account-plus-outline text-2xl" />
      </Button>
      <AddOrgMemberModal bind:this={addOrgMemberModal} orgId={org.id} />
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
      on:removeProjectFromOrg={(event) => removeProjectFromOrg(event.detail.projectId, event.detail.projectName)}
    >
      <DeleteModal
        bind:this={removeProjectFromOrgModal}
        entityName={'Project'}
        isRemoveDialog
        >
        {'Would you like to remove {projectName} from {orgName}?'}
      </DeleteModal>
    </ProjectTable>
    {:else if $queryParamValues.tab === 'members'}
    <OrgMemberTable
      shownUsers={org.members}
      showEmailColumn={canManage}
      on:openUserModal={(event) => openUserModal(event.detail)}
      on:removeMember={(event) => _deleteOrgUser(org.id, event.detail.id)}
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
            {$t('org_page.leave_org')}
            <Icon icon="i-mdi-exit-run"/>
          </Button>
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
