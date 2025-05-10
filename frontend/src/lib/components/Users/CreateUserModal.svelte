<script lang="ts">
  import { Modal } from '$lib/components/modals';
  import t from '$lib/i18n';
  import { helpLinks } from '$lib/components/help';
  import { type LexAuthUser, type RegisterResponse } from '$lib/user';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';
  import { NewTabLinkMarkdown } from '$lib/components/Markdown';
  import Icon from '$lib/icons/Icon.svelte';

  let createUserModal: Modal | undefined = $state();
  interface Props {
    handleSubmit: (
      password: string,
      passwordStrength: number,
      name: string,
      email: string,
      locale: string,
      turnstileToken: string,
    ) => Promise<RegisterResponse>;
    onSubmitted?: (submittedUser: LexAuthUser) => void;
  }

  const { handleSubmit, onSubmitted }: Props = $props();

  let formTainted = $state(false);

  export async function open(): Promise<void> {
    await createUserModal?.openModal(true, true);
  }
</script>

<Modal bind:this={createUserModal} bottom closeOnClickOutside={!formTainted}>
  <div class="alert alert-info gap-4 mb-4">
    <Icon icon="i-mdi-information-outline" size="text-2xl" />
    <div>
      <h3 class="text-lg">{$t('common.did_you_know')}</h3>
      <div>
        <NewTabLinkMarkdown
          md={$t('admin_dashboard.create_user_modal.help_create_single_guest_user', {
            helpLink: helpLinks.addProjectMember,
          })}
        />
        <NewTabLinkMarkdown
          md={$t('admin_dashboard.create_user_modal.help_create_bulk_guest_users', {
            helpLink: helpLinks.bulkAddCreate,
          })}
        />
      </div>
    </div>
  </div>
  <h1 class="text-center text-xl">{$t('admin_dashboard.create_user_modal.create_user')}</h1>
  <CreateUser
    {handleSubmit}
    allowUsernames
    skipTurnstile
    bind:formTainted
    onSubmitted={(user) => {
      if (createUserModal) {
        createUserModal.submitModal();
        onSubmitted?.(user);
      }
    }}
    submitButtonText={$t('admin_dashboard.create_user_modal.create_user')}
  />
</Modal>
