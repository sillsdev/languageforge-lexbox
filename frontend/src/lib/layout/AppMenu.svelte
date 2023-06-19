<script>
  import t from '$lib/i18n';
  import { AdminIcon, AuthenticatedUserIcon, HomeIcon, LogoutIcon } from '$lib/icons';
  import { page } from '$app/stores';
  import AdminContent from './AdminContent.svelte';
</script>

<div class="drawer-side" on:click on:keydown>
  <div class="drawer-overlay" />

  <!-- https://daisyui.com/components/menu  -->
  <ul class="menu bg-base-100 min-w-[33%]">
    <header class="prose flex flex-col items-end p-4 mb-4">
      <h2 class="mb-0">{$page.data.user?.name}</h2>
      <span class="font-light">{$page.data.user?.email}</span>
    </header>

    <li>
      <a href="/logout" data-sveltekit-preload-data="tap">
        {$t('appmenu.log_out')}
        <LogoutIcon />
      </a>
    </li>

    <div class="divider" />

    <AdminContent>
      <li>
        <a href="/admin" class="text-accent" data-sveltekit-preload-data="tap">
          {$t('admin_dashboard.title')}
          <AdminIcon />
        </a>
      </li>
    </AdminContent>

    <li>
      <a href="/" data-sveltekit-preload-data="tap">
        {$t('user_dashboard.title')}
        <HomeIcon />
      </a>
    </li>

    <li>
      <a href="/" data-sveltekit-preload-data="tap">
        {$t('user_page.title')}
        <AuthenticatedUserIcon />
      </a>
    </li>

    <div class="divider" />
  </ul>
</div>

<style>
  a {
    justify-content: flex-end;
  }
</style>
