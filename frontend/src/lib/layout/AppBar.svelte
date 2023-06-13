<script lang="ts">
  import { env } from '$env/dynamic/public';
  import t from '$lib/i18n';
  import { AuthenticatedUserIcon, UserAddOutline } from '$lib/icons';
  import { user } from '$lib/user';
  import { createEventDispatcher } from 'svelte';

  let environmentName = env.PUBLIC_ENV_NAME;
  const dispatch = createEventDispatcher();
</script>

<!-- https://daisyui.com/components/navbar -->
<header class="navbar bg-primary text-primary-content pl-6">
  <a href="/" class="navbar-start text-lg md:text-3xl tracking-wider hover:underline">
    {$t('appbar.app_name')}
  </a>
  {#if environmentName !== 'production'}
    <a href="https://public.languagedepot.org" class="alert alert-warning">
      <div class="items-center">
        <span>
          {$t('environment_warning', { environmentName })}
        </span>
      </div>
    </a>
  {/if}
  <div class="navbar-end">
    {#if $user}
      <button on:click={() => dispatch('menuopen')} class="btn btn-primary btn-circle">
        <AuthenticatedUserIcon size="text-4xl" />
      </button>
    {:else}
      <div class="tooltip tooltip-left" data-tip="Create a new account">
        <a href="/register" class="btn btn-primary btn-circle">
          <UserAddOutline />
        </a>
      </div>
    {/if}
  </div>
</header>
