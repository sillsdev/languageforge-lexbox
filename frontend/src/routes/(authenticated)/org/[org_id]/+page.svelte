<script lang="ts">
  import { DetailsPage, DetailItem, AdminContent } from '$lib/layout';

  import t, { date } from '$lib/i18n';
  import { z } from 'zod';
  import EditableText from '$lib/components/EditableText.svelte';
  import { Button, type ErrorMessage } from '$lib/forms';
  import type { PageData } from './$types';
  import { OrgRole } from '$lib/gql/types';
  import { useNotifications } from '$lib/notify';
  import { _changeOrgName, _deleteOrgUser, _deleteOrg, type OrgSearchParams } from './+page';
  import OrgTabs, { type OrgTabId } from './OrgTabs.svelte';
  import { getSearchParams, queryParam } from '$lib/util/query-params';
  import OrgMemberTable, { type Member } from '$lib/components/Orgs/OrgMemberTable.svelte';
  import { Icon, TrashIcon } from '$lib/icons';
  import ConfirmDeleteModal from '$lib/components/modals/ConfirmDeleteModal.svelte';
  import { goto } from '$app/navigation';
  import { DialogResponse } from '$lib/components/modals';
  import AddOrgMemberModal from '$lib/components/Orgs/AddOrgMemberModal.svelte';
  import { _changeOrgMemberRole } from '$lib/components/Orgs/mutations';
  import ChangeOrgMemberRoleModal from '$lib/components/Orgs/ChangeOrgMemberRoleModal.svelte';

  export let data: PageData;
  $: user = data.user;
  let orgStore = data.org;
  $: org = $orgStore;

  const queryParams = getSearchParams<OrgSearchParams>({
    tab: queryParam.string<OrgTabId>('projects'),
  });
  const { queryParamValues } = queryParams;

  $: canManage = user.isAdmin || !!org.members.find(m => m.user.id === user.id && m.role === OrgRole.Admin)

  const { notifySuccess, notifyWarning } = useNotifications();

  const orgNameValidation = z.string().trim().min(1, $t('org_page.org_name_empty_error'));

  async function updateOrgName(newName: string): Promise<ErrorMessage> {
    const result = await _changeOrgName({ orgId: org.id, name: newName });
    if (result.error) {
      return result.error.message;
    }
    notifySuccess($t('org_page.notifications.rename_org', { name: newName }));
  }

  let addOrgMemberModal: AddOrgMemberModal;
  async function openAddOrgMemberModal(): Promise<void> {
    await addOrgMemberModal.openModal();
  }

  let changeMemberRoleModal: ChangeOrgMemberRoleModal;
  async function openChangeMemberRoleModal(member: Member): Promise<void> {
    await changeMemberRoleModal.open({
      userId: member.user.id,
      name: member.user.name,
      role: member.role
    });
  }

  let deleteOrgModal: ConfirmDeleteModal;
  async function confirmDeleteOrg(): Promise<void> {
    const result = await deleteOrgModal.open(org.name, async () => {
      if (confirm(`Do you really want to delete ${org.name}? There is NO UNDO.`)) { // TODO: en.json
        const { error } = await _deleteOrg(org.id);
        return error?.message;
      } else {
        return `Deletion cancelled.`; // TODO: en.json
      }
    });
    if (result.response === DialogResponse.Submit) {
      notifyWarning($t('org_page.notifications.delete_org', { name: org.name }));
      await goto('/');
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
    <!-- TODO: Add project count once orgs can own projects -->
    <OrgTabs bind:activeTab={$queryParamValues.tab} memberCount={org.members.length} projectCount={0} />
  </div>
  <div class="py-6 px-2">
    {#if $queryParamValues.tab === 'projects'}
    Projects list will go here once orgs have projects associated with them
    {:else if $queryParamValues.tab === 'members'}
    <OrgMemberTable
      orgId={org.id}
      shownUsers={org.members}
      {canManage}
      on:openUserModal={() => console.log('TODO: user details modal not yet done')}
      on:removeMember={(event) => _deleteOrgUser(org.id, event.detail.id)}
      on:changeMemberRole={(event) => openChangeMemberRoleModal(event.detail)}
    />
    {:else if $queryParamValues.tab === 'history'}
      <div class="space-y-2">
        <DetailItem title={$t('org_page.details.created_at')} text={$date(org.createdDate)} />
        <DetailItem title={$t('org_page.details.updated_at')} text={$date(org.updatedDate)} />
      </div>
    {:else if $queryParamValues.tab === 'settings'}
      <div class="flex justify-end">
        <Button outline variant="btn-error" on:click={leaveOrg}>
          {$t('org_page.leave_org')}
          <Icon icon="i-mdi-exit-run"/>
        </Button>
      </div>
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