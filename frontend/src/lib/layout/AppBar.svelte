<script lang="ts">
  import { env } from '$env/dynamic/public';
  import t from '$lib/i18n';
  import { AuthenticatedUserIcon, UserAddOutline } from '$lib/icons';
  import { onMount } from 'svelte';
  import type { LexAuthUser } from '$lib/user';
  import { page } from '$app/stores';
  import AppLogo from '$lib/icons/AppLogo.svelte';

  onMount(() => {
    isPlaywright = localStorage.getItem('isPlaywright') === 'true';
    if (isPlaywright) environmentName = 'playwright';
  });
  let isPlaywright = false;
  let environmentName = env.PUBLIC_ENV_NAME?.toLowerCase();
  export let user: LexAuthUser | undefined;
  $: loggedIn = !!user;
</script>

<!-- https://daisyui.com/components/navbar -->
<header>
  {#if environmentName !== 'production'}
    <a href="https://public.languagedepot.org" class="flex gap-2 justify-center items-center bg-warning text-warning-content p-2 underline">
      {$t('environment_warning', { environmentName })}
      <span class="i-mdi-open-in-new text-xl shrink-0" />
    </a>
  {/if}
  <div class="navbar justify-between bg-primary text-primary-content md:pl-3 md:pr-6">
    <div class="flex">
      <AppLogo class="h-[2.5em] w-[2.5em] mr-3"/>
      <a href={loggedIn ? '/' : '/login'} class="flex flex-col text-lg md:text-3xl tracking-wider hover:underline">
        {$t('appbar.app_name')}
        <span class="text-xs md:text-[0.35em]/[2em]">
          {$t('appbar.app_subtitle')}
        </span>
      </a>
    </div>
    <div>
      {#if user}
        <!-- using a label means it works before hydration is complete -->
        <label for="drawer-toggle" class="btn btn-primary glass px-2">
          {user.name}
          <AuthenticatedUserIcon size="text-4xl" />
        </label>
      {:else}
        {#if $page.url.pathname !== '/login'}
          <a href="/login" class="btn btn-primary">
            {$t('login.button_login')}
          <span class="i-mdi-logout text-3xl"/>
          </a>
        {/if}

        {#if $page.url.pathname !== '/register'}
          <a href="/register" class="btn btn-primary">
            {$t('register.button_register')}
            <UserAddOutline/>
          </a>
        {/if}
      {/if}
    </div>
  </div>
</header>
