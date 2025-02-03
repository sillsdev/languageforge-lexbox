<script lang="ts">
  import t, { date } from '$lib/i18n';
  import { Modal } from '$lib/components/modals';
  import DevContent from '$lib/layout/DevContent.svelte';
  import UserLockedAlert from './UserLockedAlert.svelte';
  import { NULL_LABEL } from '$lib/i18n';
  import IconButton from '$lib/components/IconButton.svelte';
  import AdminContent from '$lib/layout/AdminContent.svelte';
  import {_sendNewVerificationEmailByAdmin} from '../../../routes/(authenticated)/admin/+page';
  import type {UUID} from 'crypto';
  import {useNotifications} from '$lib/notify';
  import {allPossibleFlags, type FeatureFlag} from '$lib/user';

  type User = {
    id: string;
    name: string;
    email?: string | null;
    emailVerified: boolean;
    isAdmin: boolean;
    createdDate: string | Date;
    username?: string | null;
    locked: boolean
    localizationCode: string
    updatedDate: string | Date
    lastActive: string | Date
    featureFlags: FeatureFlag[]
    canCreateProjects: boolean
    createdBy?: Partial<User> | null
  };
  let userDetailsModal: Modal;
  let user: User;

  export async function open(_user: User): Promise<void> {
    user = _user;
    await userDetailsModal.openModal(true, true);
  }

  const { notifySuccess } = useNotifications();

  var sendingVerificationEmail = false;
  async function sendVerificationEmail(user: User): Promise<void> {
    sendingVerificationEmail = true;
    await _sendNewVerificationEmailByAdmin(user.id as UUID);
    sendingVerificationEmail = false;
    notifySuccess($t('admin_dashboard.notifications.verification_email_sent', { email: user.email ?? '' }));
  }
</script>

<Modal bind:this={userDetailsModal} bottom>
  <div class="p-4">
    <h2 class="text-secondary">
      <span class="text-2xl">
        {user.name}
      </span>
    </h2>
    <div class="divider" />
    <UserLockedAlert locked={user.locked} />
    <div class="grid grid-cols-2 gap-4">
      <div>
        <h3>{$t('admin_dashboard.column_email')}</h3>
        <p class="value flex items-center gap-2 text-left">
          {#if user.email}
            {user.email}
            {#if !user.emailVerified}
              <span
                class="tooltip text-warning text-md shrink-0 leading-0"
                data-tip={$t('admin_dashboard.email_not_verified')}>
                <span class="i-mdi-help-circle-outline" />
              </span>
              <AdminContent>
                <div class="tooltip" data-tip={$t('admin_dashboard.resend_verification_email')}>
                  <IconButton
                    size="btn-sm"
                    icon="i-mdi-email-sync"
                    outline={false}
                    variant="btn-primary"
                    loading={sendingVerificationEmail}
                    on:click={() => sendVerificationEmail(user)}
                  />
                </div>
              </AdminContent>
            {/if}
          {:else}
            –
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
          {$date(user.createdDate)}
        </p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.column_login')}</h3>
        <p class="value">
          {user.username ?? NULL_LABEL}
        </p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.last_active')}</h3>
        <p class="value">{$date(user.lastActive)}</p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.can_create_projects')}</h3>
        <p class="value" class:!text-success={user.canCreateProjects}>{user.canCreateProjects ? $t('common.yes') : $t('common.no')}</p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.updated')}</h3>
        <p class="value">{$date(user.updatedDate)}</p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.locale')}</h3>
        <p class="value">{user.localizationCode}</p>
      </div>
      <div>
        <h3>{$t('admin_dashboard.user_details_modal.createdBy')}</h3>
        <p class="value">{user.createdBy?.name  ?? NULL_LABEL}</p>
      </div>
      <AdminContent>
      <div>
        Feature flags:
        <ul>
          {#each allPossibleFlags as flag}
            <li><input
              type="checkbox"
              name="featureFlags"
              value={flag}
              bind:group={user.featureFlags}
            > {flag}</li>
          {/each}
        </ul>
      </div>
      </AdminContent>
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
