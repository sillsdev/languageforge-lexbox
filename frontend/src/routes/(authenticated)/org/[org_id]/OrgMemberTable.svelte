<script lang="ts">
  import { Button } from '$lib/forms';
  import t from '$lib/i18n';
  import { Icon } from '$lib/icons';
  import { createEventDispatcher } from 'svelte';
  import FormatUserOrgRole from '$lib/components/Orgs/FormatUserOrgRole.svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import type { OrgUser, User } from './+page';

  export let shownUsers: OrgUser[];
  export let showEmailColumn: boolean = true;

  const dispatch = createEventDispatcher<{
    openUserModal: User,
    changeMemberRole: OrgUser,
    removeMember: User,
  }>();

</script>

<div class="overflow-x-auto @container scroll-shadow">
  <table class="table table-lg">
    <thead>
      <tr class="bg-base-200">
        <th>
          {$t('admin_dashboard.column_name')}
          <span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2" />
        </th>
        {#if showEmailColumn}
        <th>{$t('admin_dashboard.column_email_or_login')}</th>
        {/if}
        <th class="@2xl:table-cell">
          {$t('admin_dashboard.column_role')}
        </th>
        <th>
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
            </div>
          </td>
          {#if showEmailColumn}
          <td>
            <span class="inline-flex items-center gap-2 text-left max-w-40">
              <span class="max-width-full overflow-hidden text-ellipsis" title={user.email ?? user.username}>
                {user.email ?? user.username}
              </span>
            </span>
          </td>
          {/if}
          <td class="@2xl:table-cell">
            <FormatUserOrgRole role={member.role} />
          </td>
          {#if showEmailColumn}
          <td class="p-0">
            <Dropdown>
              <button class="btn btn-ghost btn-square">
                <span class="i-mdi-dots-vertical text-lg" />
              </button>
              <ul slot="content" class="menu">
                <li>
                  <button class="whitespace-nowrap" on:click={() => dispatch('changeMemberRole', member)}>
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
              </ul>
            </Dropdown>
          </td>
          {/if}
        </tr>
      {/each}
    </tbody>
  </table>
</div>
