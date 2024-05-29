<script lang="ts">
  import { Button } from '$lib/forms';
  import t from '$lib/i18n';
  import { AdminIcon, Icon } from '$lib/icons';
  import { createEventDispatcher } from 'svelte';
  import Dropdown from '../Dropdown.svelte';
  import type { User } from '../../../routes/(authenticated)/admin/+page';
  import type { OrgRole } from '$lib/gql/types';
  import OrgRoleSelect from '$lib/forms/OrgRoleSelect.svelte';
  import OrgRoleText from './OrgRoleText.svelte';
  import AddOrgMember from '../../../routes/(authenticated)/org/[org_id]/AddOrgMember.svelte';

  type Member = {
    id: string
    user: User
    role: OrgRole
  };

  export let orgId: string;

  export let shownUsers: Member[];
  export let canManage = false;

  const dispatch = createEventDispatcher();
</script>

<table class="table table-lg">
  <thead>
    <tr class="bg-base-200">
      <th>
        {$t('admin_dashboard.column_name')}
        <span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2" />
      </th>
      <th>{$t('admin_dashboard.column_email')}</th>
      <th class="@2xl:table-cell">
        {$t('admin_dashboard.column_role')}
      </th>
      <th>
        {#if canManage}
          <AddOrgMember {orgId} />
          <!-- TODO: That might be too big. Maybe like this?
          <button
            class="btn btn-sm btn-success btn-square"
            on:click={() => dispatch('addMember')}>
            <span class="i-mdi-plus text-2xl" />
          </button>
          Or else move it to the bottom of the table. -->
        {/if}
      </th>
    </tr>
  </thead>
  <tbody>
    {#each shownUsers as member}
      <tr>
        <td>
          <div class="flex items-center gap-2 max-w-40 @xl:max-w-52">
            <Button variant="btn-ghost" size="btn-sm" class="max-w-full" on:click={() => dispatch('openUserModal', member.user)}>
              <span class="max-width-full overflow-x-clip text-ellipsis" title={member.user.name}>
                {member.user.name}
              </span>
              <Icon icon="i-mdi-card-account-details-outline" />
            </Button>
            {#if member.user.locked}
              <span
                  class="tooltip text-warning text-xl leading-0"
                  data-tip={$t('admin_dashboard.user_is_locked')}>
                <Icon icon="i-mdi-lock" />
              </span>
            {/if}
            {#if member.user.isAdmin}
              <span
                  class="tooltip text-accent text-xl leading-0"
                  data-tip={$t('user_types.admin')}>
                  <AdminIcon size="text-xl" />
              </span>
            {/if}
          </div>
        </td>
        <td>
          <span class="inline-flex items-center gap-2 text-left max-w-40">
            {#if member.user.email}
              <span class="max-width-full overflow-hidden text-ellipsis" title={member.user.email}>
                {member.user.email}
              </span>
              {#if !member.user.emailVerified}
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
        <td class="@2xl:table-cell">
          {#if canManage}
          <OrgRoleSelect bind:value={member.role} id={`role-select-${member.id}`}></OrgRoleSelect>
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
                <button class="whitespace-nowrap" on:click={() => dispatch('editUser', member.user)}>
                  <Icon icon="i-mdi-pencil-outline" />
                  {$t('admin_dashboard.form_modal.title')}
                </button>
              </li>
              <li>
                <button class="whitespace-nowrap" on:click={() => dispatch('removeMember', member.user)}>
                  <Icon icon="i-mdi-account-remove" />
                  {$t('org_page.remove_user.remove_button')}
                </button>
              </li>
              <!-- TODO: Do we want this on the org page? -->
              <!-- <li>
                <button class="whitespace-nowrap" on:click={() => dispatch('filterProjectsByUser', member.user)}>
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
