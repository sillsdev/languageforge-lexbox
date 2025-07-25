<script lang="ts">
  import { env } from '$env/dynamic/public';
  import t from '$lib/i18n';
  import { AuthenticatedUserIcon, UserAddOutline } from '$lib/icons';
  import { onMount } from 'svelte';
  import type { LexAuthUser } from '$lib/user';
  import { page } from '$app/state';
  import AppLogo from '$lib/icons/AppLogo.svelte';

  onMount(() => {
    isPlaywright = localStorage.getItem('isPlaywright') === 'true';
    if (isPlaywright) environmentName = 'playwright';
  });
  let isPlaywright = false;
  let environmentName = $state(env.PUBLIC_ENV_NAME?.toLowerCase());
  interface Props {
    user: LexAuthUser | null;
  }

  const { user }: Props = $props();
  let loggedIn = $derived(!!user);
</script>

<!-- https://daisyui.com/components/navbar -->
<header>
  {#if environmentName !== 'production'}
    <a href="https://lexbox.org" class="flex gap-2 justify-center items-center bg-warning text-warning-content p-2 underline">
      {$t('environment_warning', { environmentName })}
      <span class="i-mdi-open-in-new text-xl shrink-0"></span>
    </a>
  {/if}
  <div class="navbar justify-between bg-primary text-primary-content md:pl-3 md:pr-6">
    <a id="home" href={loggedIn ? '/' : '/login'} class="flex btn btn-primary text-left font-normal px-2">
      <AppLogo class="h-[3em] w-[3em]" mono />
      <div class="flex flex-col text-2xl md:text-3xl tracking-wider">
        <span class="md:leading-none">{$t('appbar.app_name')}</span>
        <span class="text-[0.35em]/[2em] max-xs:hidden">
          {$t('appbar.app_subtitle')}
        </span>
      </div>
    </a>
    <div>
      {#if user}
        <!-- using a label means it works before hydration is complete -->
        <label for="drawer-toggle" class="btn btn-primary glass px-2">
          {user.name}
          <AuthenticatedUserIcon size="text-4xl" />
        </label>
      {:else}
        {#if page.url.pathname !== '/login'}
          <a href="/login" class="btn btn-primary">
            {$t('login.button_login')}
          <span class="i-mdi-logout text-3xl"></span>
          </a>
        {/if}

        {#if page.url.pathname !== '/register'}
          <a href="/register" class="btn btn-primary">
            {$t('register.button_register')}
            <UserAddOutline/>
          </a>
        {/if}
      {/if}
    </div>
  </div>
</header>
