<script lang="ts">
  import {TitlePage} from '$lib/layout';
  import t from '$lib/i18n';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';
  import {goto} from '$app/navigation';
  import {resolve} from '$app/paths';
  import {acceptInvitation} from '$lib/user';

  async function onSubmit(): Promise<void> {
    await goto(resolve('/home'), { invalidateAll: true }); // invalidate so we get the user from the server
  }
</script>

<TitlePage title={$t('accept_invitation.title')}>
  <CreateUser
    handleSubmit={acceptInvitation}
    onSubmitted={onSubmit}
    errorOnChangingEmail={$t('register.changing_email_not_allowed')}
  />
</TitlePage>
