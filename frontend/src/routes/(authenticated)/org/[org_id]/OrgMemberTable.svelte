<script lang="ts">
  import { Button } from '$lib/forms';
  import t from '$lib/i18n';
  import { Icon } from '$lib/icons';
  import { createEventDispatcher } from 'svelte';
  import FormatUserOrgRole from '$lib/components/Orgs/FormatUserOrgRole.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import { _deleteOrgUser, type Org, type OrgUser, type User } from './+page';
  import DeleteModal from '$lib/components/modals/DeleteModal.svelte';
  import { useNotifications } from '$lib/notify';
  import type { LexAuthUser } from '$lib/user';
  import { OrgRole } from '$lib/gql/types';

  interface Props {
    org: Org;
    user: LexAuthUser;
    shownUsers: OrgUser[];
    canManage: boolean;
  }

  let { org, user, shownUsers, canManage }: Props = $props();

  const { notifyWarning } = useNotifications();

  const dispatch = createEventDispatcher<{
    openUserModal: User;
    changeMemberRole: OrgUser;
  }>();

  let removeMemberModal: DeleteModal = $state()!;
  let memberToRemove: string = $state('');

  async function removeMember(member: User): Promise<void> {
    memberToRemove = member.name;
    const removed = await removeMemberModal.prompt(async () => {
      const { error } = await _deleteOrgUser(org.id, member.id);
      if (error) return $t('org_page.remove_member.error', { memberName: member.name });
    });

    if (removed) {
      notifyWarning($t('org_page.remove_member.success', { memberName: member.name }));
    }
  }
</script>

<div class="overflow-x-auto @container scroll-shadow">
  <table class="table table-lg">
    <thead>
      <tr class="bg-base-200">
        <th>
          {$t('admin_dashboard.column_name')}
          <span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2"></span>
        </th>
        {#if canManage}
          <th>{$t('admin_dashboard.column_email_or_login')}</th>
        {/if}
        <th class="@2xl:table-cell">
          {$t('admin_dashboard.column_role')}
        </th>
        <th> </th>
      </tr>
    </thead>
    <tbody>
      {#each shownUsers as member}
        {@const memberUser = member.user}
        {@const isOrgAdmin = member.role === OrgRole.Admin}
        <tr>
          <td>
            <div class="flex items-center gap-2 max-w-40 @xl:max-w-52">
              {#if canManage}
                <Button
                  variant="btn-ghost"
                  size="btn-sm"
                  class="max-w-full"
                  on:click={() => dispatch('openUserModal', memberUser)}
                >
                  <span class="x-ellipsis" title={memberUser.name}>
                    {memberUser.name}
                  </span>
                  <Icon icon="i-mdi-card-account-details-outline" />
                </Button>
              {:else}
                <span class="x-ellipsis" title={memberUser.name}>
                  {memberUser.name}
                </span>
              {/if}
            </div>
          </td>
          {#if canManage}
            <td>
              <span class="inline-flex items-center gap-2 text-left max-w-40">
                <span class="x-ellipsis" title={memberUser.email ?? memberUser.username}>
                  {memberUser.email ?? memberUser.username}
                </span>
              </span>
            </td>
          {/if}
          <td
            class="@2xl:table-cell"
            class:text-primary={isOrgAdmin}
            class:font-bold={isOrgAdmin}
            class:dark:brightness-150={isOrgAdmin}
          >
            <FormatUserOrgRole role={member.role} />
          </td>
          {#if user.isAdmin || (canManage && memberUser.id !== user.id)}
            <td class="p-0">
              <Dropdown>
                <button class="btn btn-ghost btn-square" aria-label={$t('common.actions')}>
                  <span class="i-mdi-dots-vertical text-lg"></span>
                </button>
                {#snippet content()}
                  <ul class="menu">
                    <li>
                      <button class="whitespace-nowrap" onclick={() => dispatch('changeMemberRole', member)}>
                        <Icon icon="i-mdi-pencil-outline" />
                        {$t('org_page.edit_member_role')}
                      </button>
                    </li>
                    <li>
                      <button class="whitespace-nowrap text-error" onclick={() => removeMember(memberUser)}>
                        <Icon icon="i-mdi-account-remove" />
                        {$t('org_page.remove_member.remove')}
                      </button>
                    </li>
                  </ul>
                {/snippet}
              </Dropdown>
            </td>
          {/if}
        </tr>
      {/each}
    </tbody>
  </table>
</div>

<DeleteModal bind:this={removeMemberModal} entityName={$t('org_page.remove_member.member')} isRemoveDialog>
  {$t('org_page.remove_member.confirm_message', { memberName: memberToRemove })}
</DeleteModal>
