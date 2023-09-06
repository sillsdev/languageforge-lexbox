<script lang="ts">
  import { env } from '$env/dynamic/public';
  import t from '$lib/i18n';
  import { AuthenticatedUserIcon, UserAddOutline } from '$lib/icons';
  import { page } from '$app/stores';
  import { createEventDispatcher } from 'svelte';

  let environmentName = env.PUBLIC_ENV_NAME;
  const dispatch = createEventDispatcher();
  let loggedIn = $page.data.user != null;
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
      {#if loggedIn}
        <button on:click={() => dispatch('menuopen')} class="btn btn-primary btn-circle">
          <AuthenticatedUserIcon size="text-4xl" />
        </button>
      {:else}
        <div class="tooltip tooltip-left" data-tip={$t('register.create_new_account')}>
          <a href="/register" class="btn btn-primary btn-circle">
            <UserAddOutline />
          </a>
        </div>
      {/if}
    </div>
  </div>
</header>
