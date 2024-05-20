<script lang="ts">
  import { TitlePage } from '$lib/layout';
  import t from '$lib/i18n';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';
  import { goto } from '$app/navigation';

  async function onSubmit(): Promise<void> {
    await goto('/home', { invalidateAll: true }); // invalidate so we get the user from the server
  }
</script>

<TitlePage title={$t('register.title')}>
  <CreateUser endpoint="register" on:submitted={onSubmit} />
</TitlePage>
