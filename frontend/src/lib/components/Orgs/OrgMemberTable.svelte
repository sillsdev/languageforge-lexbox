<script lang="ts">
  import { Button } from '$lib/forms';
  import t from '$lib/i18n';
  import { Icon } from '$lib/icons';
  import { createEventDispatcher } from 'svelte';
  import Dropdown from '../Dropdown.svelte';
  import type { OrgRole, User } from '$lib/gql/types';
  import OrgRoleText from './OrgRoleText.svelte';
  import IconButton from '../IconButton.svelte';
  import AddOrgMemberModal from './AddOrgMemberModal.svelte';
  import OrgRoleSelect from '$lib/forms/OrgRoleSelect.svelte';

  type TableUser = Pick<User, 'name' | 'locked' | 'username' | 'email' | 'emailVerified'>;

  type Member = {
    id: string
    user: TableUser
    role: OrgRole
  };

  export let orgId: string;

  export let shownUsers: Member[];
  export let canManage = false;

  let editingUser: TableUser|undefined = undefined;

  const dispatch = createEventDispatcher();

  let addOrgMemberModal: AddOrgMemberModal;
  async function openAddOrgMemberModal(): Promise<void> {
    await addOrgMemberModal.openModal();
  }
</script>

<div class="overflow-x-auto @container scroll-shadow">
  <table class="table table-lg">
    <thead>
      <tr class="bg-base-200">
        <th>
          {$t('admin_dashboard.column_name')}
          <span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2" />
        </th>
        <th>{$t('admin_dashboard.column_email_or_login')}</th>
        <th class="@2xl:table-cell">
          {$t('admin_dashboard.column_role')}
        </th>
        <th class="px-2 py-1">
          {#if canManage}
            <Dropdown>
              <IconButton icon="i-mdi-plus" variant="btn-success" size="btn-sm" outline={false} />
              <ul slot="content" class="menu">
                <li>
                  <button class="whitespace-nowrap text-success" on:click={openAddOrgMemberModal}>
                    <Icon icon="i-mdi-plus" />
                    {$t('org_page.add_user.add_button')}
                  </button>
                </li>
                <!-- TODO: Do we want this on the org page? -->
                <!-- <li>
                  <button class="whitespace-nowrap" on:click={() => dispatch('filterProjectsByUser', user)}>
                    <Icon icon="i-mdi-filter-outline" />
                    {$t('project.filter.filter_user_projects')}
                  </button>
                </li> -->
              </ul>
            </Dropdown>
          {/if}
        </th>
      </tr>
    </thead>
    <tbody>
      {#each shownUsers as member}
      {@const user = member.user}
        <tr>
          <td>
            <div class="flex items-center gap-2 max-w-40 @xl:max-w-52">
              <Button variant="btn-ghost" size="btn-sm" class="max-w-full" on:click={() => dispatch('openUserModal', user)}>
                <span class="max-width-full overflow-x-clip text-ellipsis" title={user.name}>
                  {user.name}
                </span>
                <Icon icon="i-mdi-card-account-details-outline" />
              </Button>
              {#if user.locked}
                <span
                    class="tooltip text-warning text-xl leading-0"
                    data-tip={$t('admin_dashboard.user_is_locked')}>
                  <Icon icon="i-mdi-lock" />
                </span>
              {/if}
            </div>
          </td>
          <td>
            <span class="inline-flex items-center gap-2 text-left max-w-40">
              <span class="max-width-full overflow-hidden text-ellipsis" title={user.email ?? user.username}>
                {user.email ?? user.username}
              </span>
              {#if user.email && !user.emailVerified}
                <span
                  class="tooltip text-warning text-xl shrink-0 leading-0"
                  data-tip={$t('admin_dashboard.email_not_verified')}>
                  <span class="i-mdi-help-circle-outline" />
                </span>
              {/if}
            </span>
          </td>
          <td class="@2xl:table-cell">
            {#if editingUser == user}
            <OrgRoleSelect bind:value={member.role} on:change={() => {dispatch('changeMemberRole', member); editingUser = undefined}} />
            {:else}
            <OrgRoleText value={member.role} />
            {/if}
          </td>
          <td class="p-0">
            <Dropdown>
              <button class="btn btn-ghost btn-square">
                <span class="i-mdi-dots-vertical text-lg" />
              </button>
              <ul slot="content" class="menu">
                <li>
                  <button class="whitespace-nowrap" on:click={() => editingUser = user}>
                    <Icon icon="i-mdi-pencil-outline" />
                    {$t('org_page.edit_member_role')}
                  </button>
                </li>
                <li>
                  <button class="whitespace-nowrap" on:click={() => dispatch('removeMember', user)}>
                    <Icon icon="i-mdi-account-remove" />
                    {$t('org_page.remove_member')}
                  </button>
                </li>
                <!-- TODO: Do we want this on the org page? -->
                <!-- <li>
                  <button class="whitespace-nowrap" on:click={() => dispatch('filterProjectsByUser', user)}>
                    <Icon icon="i-mdi-filter-outline" />
                    {$t('project.filter.filter_user_projects')}
                  </button>
                </li> -->
              </ul>
            </Dropdown>
          </td>
        </tr>
      {/each}
    </tbody>
  </table>
</div>

<AddOrgMemberModal bind:this={addOrgMemberModal} orgId={orgId} />
