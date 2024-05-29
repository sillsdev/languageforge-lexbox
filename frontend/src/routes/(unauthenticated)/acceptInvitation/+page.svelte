<script lang="ts">
  import { TitlePage } from '$lib/layout';
  import t from '$lib/i18n';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';
  import { goto } from '$app/navigation';
  import { acceptInvitation } from '$lib/user';

  async function onSubmit(): Promise<void> {
    await goto('/home', { invalidateAll: true }); // invalidate so we get the user from the server
  }
</script>

<TitlePage title={$t('accept_invitation.title')}>
  <CreateUser handleSubmit={acceptInvitation} on:submitted={onSubmit} />
</TitlePage>
