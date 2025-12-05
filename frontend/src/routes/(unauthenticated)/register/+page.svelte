<script lang="ts">
  import {TitlePage} from '$lib/layout';
  import t from '$lib/i18n';
  import CreateUser from '$lib/components/Users/CreateUser.svelte';
  import {goto} from '$app/navigation';
  import {resolve} from '$app/paths';
  import {register} from '$lib/user';
  import RegisterWithGoogleButton from '$lib/components/RegisterWithGoogleButton.svelte';

  async function onSubmit(): Promise<void> {
    await goto(resolve('/home'), { invalidateAll: true }); // invalidate so we get the user from the server
  }
</script>

<TitlePage title={$t('register.title')}>
  <div class="flex flex-col pb-2">
    <RegisterWithGoogleButton href="/api/login/google"/>
  </div>
  <div class="divider lowercase">{$t('common.or')}</div>
  <CreateUser handleSubmit={register} onSubmitted={onSubmit} />
</TitlePage>
