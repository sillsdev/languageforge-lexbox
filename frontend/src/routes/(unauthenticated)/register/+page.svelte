<script lang="ts">
  import { TitlePage } from '$lib/layout';
  import t from '$lib/i18n';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';
  import { goto } from '$app/navigation';
  import { register } from '$lib/user';
  import RegisterWithGoogleButton from '$lib/components/RegisterWithGoogleButton.svelte';
  import { page } from '$app/stores';

  async function onSubmit(): Promise<void> {
    await goto('/home', { invalidateAll: true }); // invalidate so we get the user from the server
  }
</script>

<TitlePage title={$t('register.title')}>
  <RegisterWithGoogleButton href={`/api/login/google?redirectTo=${$page.url.pathname}`}/>
  <div class="divider lowercase">{$t('common.or')}</div>
  <CreateUser handleSubmit={register} on:submitted={onSubmit} />
</TitlePage>
