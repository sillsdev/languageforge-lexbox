<script lang="ts">
  import { env } from '$env/dynamic/public';
  import t from '$lib/i18n';
  import { AuthenticatedUserIcon, UserAddOutline } from '$lib/icons';
  import { createEventDispatcher } from 'svelte';
  import type {LexAuthUser} from '$lib/user';
  import Button from '$lib/forms/Button.svelte';
  import {page} from '$app/stores';

  let environmentName = env.PUBLIC_ENV_NAME;
  const dispatch = createEventDispatcher();
  export let user: LexAuthUser | undefined;
  $: loggedIn = !!user;
</script>

<!-- https://daisyui.com/components/navbar -->
<header>
  {#if environmentName !== 'production'}
    <a href="https://public.languagedepot.org" class="flex gap-2 justify-center items-center bg-warning text-warning-content p-2 underline">
      {$t('environment_warning', { environmentName })}
      <span class="i-mdi-open-in-new text-xl" />
    </a>
  {/if}
  <div class="navbar justify-between bg-primary text-primary-content md:pl-6">
    <a href={loggedIn ? '/' : '/login'} class="text-lg md:text-3xl tracking-wider hover:underline">
      {$t('appbar.app_name')}
    </a>
    <div>
      {#if user}
        <Button on:click={() => dispatch('menuopen')} class="btn-primary normal-case">
          {user.name}
          <AuthenticatedUserIcon size="text-4xl" />
        </Button>
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
