<script lang="ts">
  import { Modal } from '$lib/components/modals';
  import t from '$lib/i18n';
  import { helpLinks } from '$lib/components/help';
  import { type RegisterResponse } from '$lib/user';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';
  import Markdown from 'svelte-exmarkdown';
  import { NewTabLinkRenderer } from '$lib/components/Markdown';
  import Icon from '$lib/icons/Icon.svelte';

  let createUserModal: Modal;
  export let handleSubmit: (password: string, passwordStrength: number, name: string, email: string, locale: string, turnstileToken: string) => Promise<RegisterResponse>;

  export async function open(): Promise<void> {
    await createUserModal.openModal(true, true);
  }
</script>

<Modal bind:this={createUserModal} bottom>
  <div class="alert alert-info gap-4 mb-4">
    <Icon icon="i-mdi-info-outline" size="text-2xl" />
    <div>
      <h3 class="text-lg">{$t('common.did_you_know')}</h3>
      <div>
        <Markdown
          md={$t('admin_dashboard.create_user_modal.help_create_single_guest_user', { helpLink: helpLinks.addProjectMember })}
          plugins={[{ renderer: { a: NewTabLinkRenderer } }]}
        />
        <Markdown
          md={$t('admin_dashboard.create_user_modal.help_create_bulk_guest_users', { helpLink: helpLinks.bulkAddCreate })}
          plugins={[{ renderer: { a: NewTabLinkRenderer } }]}
        />
      </div>
    </div>
  </div>
  <h1 class="text-center text-xl">{$t('admin_dashboard.create_user_modal.create_user')}</h1>
  <CreateUser {handleSubmit} allowUsernames skipTurnstile
    on:submitted={() => createUserModal.submitModal()}
    submitButtonText={$t('admin_dashboard.create_user_modal.create_user')}
  />
</Modal>
