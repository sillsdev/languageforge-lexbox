<script lang="ts">
  import { Button } from '$lib/forms';
  import t from '$lib/i18n';
  import { AdminIcon, Icon } from '$lib/icons';
  import Dropdown from '$lib/components/Dropdown.svelte';
  import type { User } from '../../../routes/(authenticated)/admin/+page';

  interface Props {
    shownUsers: User[];
    onOpenUserModal: (user: User) => void;
    onEditUser: (user: User) => void;
    onFilterProjectsByUser: (user: User) => void;
  }

  const { shownUsers, onOpenUserModal, onEditUser, onFilterProjectsByUser }: Props = $props();
</script>

<table class="table table-lg">
  <thead>
    <tr class="bg-base-200">
      <th>
        {$t('admin_dashboard.column_name')}
        <span class="i-mdi-sort-ascending text-xl align-[-5px] ml-2"></span>
      </th>
      <th class="hidden @2xl:table-cell">
        {$t('admin_dashboard.column_login')}
      </th>
      <th>{$t('admin_dashboard.column_email')}</th>
      <th></th>
    </tr>
  </thead>
  <tbody>
    {#each shownUsers as user}
      <tr>
        <td>
          <div class="flex items-center gap-2 max-w-40 @xl:max-w-52">
            <Button variant="btn-ghost" size="btn-sm" class="max-w-full" onclick={() => onOpenUserModal(user)}>
              <span class="x-ellipsis" title={user.name}>
                {user.name}
              </span>
              <Icon icon="i-mdi-card-account-details-outline" />
            </Button>
            {#if user.locked}
              <span class="tooltip text-warning text-xl leading-0" data-tip={$t('admin_dashboard.user_is_locked')}>
                <Icon icon="i-mdi-lock" />
              </span>
            {/if}
            {#if user.isAdmin}
              <span class="tooltip text-accent text-xl leading-0" data-tip={$t('user_types.admin')}>
                <AdminIcon size="text-xl" />
              </span>
            {/if}
          </div>
        </td>
        <td class="hidden @2xl:table-cell">
          {#if user.username}
            <span class="inline-flex max-w-40">
              <span class="x-ellipsis" title={user.username}>
                {user.username}
              </span>
            </span>
          {:else}
            –
          {/if}
        </td>
        <td>
          <span class="inline-flex items-center gap-2 text-left max-w-40">
            {#if user.email}
              <span class="x-ellipsis" title={user.email}>
                {user.email}
              </span>
              {#if !user.emailVerified}
                <span
                  class="tooltip text-warning text-xl shrink-0 leading-0"
                  data-tip={$t('admin_dashboard.email_not_verified')}
                >
                  <span class="i-mdi-help-circle-outline"></span>
                </span>
              {/if}
            {:else}
              –
            {/if}
          </span>
        </td>
        <td class="p-0">
          <Dropdown>
            <button class="btn btn-ghost btn-square" aria-label={$t('common.actions')}>
              <span class="i-mdi-dots-vertical text-lg"></span>
            </button>
            {#snippet content()}
              <ul class="menu">
                <li>
                  <button class="whitespace-nowrap" onclick={() => onEditUser(user)}>
                    <Icon icon="i-mdi-pencil-outline" />
                    {$t('admin_dashboard.form_modal.title')}
                  </button>
                </li>
                <li>
                  <button class="whitespace-nowrap" onclick={() => onFilterProjectsByUser(user)}>
                    <Icon icon="i-mdi-filter-outline" />
                    {$t('project.filter.filter_user_projects')}
                  </button>
                </li>
              </ul>
            {/snippet}
          </Dropdown>
        </td>
      </tr>
    {/each}
  </tbody>
</table>
