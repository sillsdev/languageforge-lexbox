<script lang="ts">
  import { Modal } from '$lib/components/modals';
  import t from '$lib/i18n';
  import { helpLinks } from '$lib/components/help';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';
  import Markdown from 'svelte-exmarkdown';
  import { NewTabLinkRenderer } from '$lib/components/Markdown';

  let createUserModal: Modal;

  export async function open(): Promise<void> {
    await createUserModal.openModal(true, true);
  }
</script>

<Modal bind:this={createUserModal} bottom>
  <Markdown md={$t('admin_dashboard.create_user_modal.help_create_single_guest_user', helpLinks)} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
  <Markdown md={$t('admin_dashboard.create_user_modal.help_create_bulk_guest_users', helpLinks)} plugins={[{ renderer: { a: NewTabLinkRenderer } }]} />
  <h1 class="text-center text-xl">{$t('admin_dashboard.create_user_modal.create_user')}</h1>
  <CreateUser autoLogin={false} onSubmit={() => createUserModal.submitModal()} />
</Modal>
