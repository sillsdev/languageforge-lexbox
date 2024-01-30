<script lang="ts">
  import t from '$lib/i18n/index.js';
  import { Modal } from '$lib/components/modals/index.js';
  import FormatDate from '$lib/components/FormatDate.svelte';
  import DevContent from '$lib/layout/DevContent.svelte';
  import { Icon } from '$lib/icons';
  type User = {
    id: string;
    name: string;
    email: string;
    emailVerified: boolean;
    isAdmin: boolean;
    createdDate: string | Date;
    username?: string | null;
    locked: boolean
    localizationCode: string
    updatedDate: string | Date
    lastActive: string | Date
    canCreateProjects: boolean
  };
  let userDetailsModal: Modal;
  let user: User;

  export async function open(_user: User): Promise<void> {
    user = _user;
    await userDetailsModal.openModal(true, true);
  }
</script>

<Modal bind:this={userDetailsModal} bottom>
  <div class="p-4">
    <h2 class="text-secondary">
      <span class="text-2xl">
        {user.name}
      </span>
      {#if user.locked}
        <span class="inline-flex gap-2 items-center ml-2">
          <Icon icon="i-mdi-lock" color="text-warning" />
          <span class="text-warning">
            {$t('admin_dashboard.user_is_locked')}
          </span>
        </span>
      {/if}
    </h2>
    <div class="divider" />
    <div class="grid grid-cols-2 gap-4">
      <div>
        <h3>{$t('admin_dashboard.column_email')}</h3>
        <p class="value flex items-center gap-2 text-left">
          {user.email}
          {#if !user.emailVerified}
          <span
            class="tooltip text-warning text-md shrink-0 leading-0"
            data-tip={$t('admin_dashboard.email_not_verified')}>
            <span class="i-mdi-help-circle-outline" />
          </span>
        {/if}
        </p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.column_role')}</h3>
        <p class="value">{user.isAdmin ? $t('user_types.admin') : $t('user_types.user')}</p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.registered')}</h3>
        <p class="value">
          <FormatDate date={user.createdDate} />
        </p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.column_login')}</h3>
        <p class="value">
          {user.username ?? '-'}
        </p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.last_active')}</h3>
        <p class="value"><FormatDate date={user.lastActive} /></p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.can_create_projects')}</h3>
        <p class="value" class:!text-success={user.canCreateProjects}>{user.canCreateProjects ? $t('common.yes') : $t('common.no')}</p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.updated')}</h3>
        <p class="value"><FormatDate date={user.updatedDate} /></p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.locale')}</h3>
        <p class="value">{user.localizationCode}</p>
      </div>
      <DevContent>
        <div>
          <h3>ID</h3>
          <p class="value">{user.id}</p>
        </div>
      </DevContent>
    </div>
  </div>
</Modal>

<style lang="postcss">
  .value {
    @apply text-secondary text-lg;
  }
</style>
