<script lang="ts">
  import { Button } from '$lib/forms';
  import t from '$lib/i18n';
  import { AdminIcon, Icon } from '$lib/icons';
  import { createEventDispatcher } from 'svelte';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import type { User } from '../../../routes/(authenticated)/admin/+page';

  export let shownUsers: User[];

  const dispatch = createEventDispatcher<{
    openUserModal: User
    editUser: User
    filterProjectsByUser: User
  }>();
</script>

<table class="table table-lg">
  <thead>
    <tr class="bg-base-200">
      <th>
        {$t('admin_dashboard.column_name')}
        <span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2" />
      </th>
      <th class="hidden @2xl:table-cell">
        {$t('admin_dashboard.column_login')}
      </th>
      <th>{$t('admin_dashboard.column_email')}</th>
      <th />
    </tr>
  </thead>
  <tbody>
    {#each shownUsers as user}
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
            {#if user.isAdmin}
              <span
                  class="tooltip text-accent text-xl leading-0"
                  data-tip={$t('user_types.admin')}>
                  <AdminIcon size="text-xl" />
              </span>
            {/if}
          </div>
        </td>
        <td class="hidden @2xl:table-cell">
          {#if user.username}
            {user.username}
          {:else}
            –
          {/if}
        </td>
        <td>
          <span class="inline-flex items-center gap-2 text-left max-w-40">
            {#if user.email}
              <span class="max-width-full overflow-hidden text-ellipsis" title={user.email}>
                {user.email}
              </span>
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
                <button class="whitespace-nowrap" on:click={() => dispatch('editUser', user)}>
                  <Icon icon="i-mdi-pencil-outline" />
                  {$t('admin_dashboard.form_modal.title')}
                </button>
              </li>
              <li>
                <button class="whitespace-nowrap" on:click={() => dispatch('filterProjectsByUser', user)}>
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
