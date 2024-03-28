<script lang="ts">
  import t from '$lib/i18n';
  import {AdminIcon, AuthenticatedUserIcon, HomeIcon, LogoutIcon} from '$lib/icons';
  import AdminContent from './AdminContent.svelte';
  import Badge from '$lib/components/Badges/Badge.svelte';
  import {APP_VERSION} from '$lib/util/version';
  import type {LexAuthUser} from '$lib/user';

  export let serverVersion: string;
  export let apiVersion: string | null;
  export let user: LexAuthUser;
</script>

<!-- https://daisyui.com/components/menu  -->
<ul class="menu bg-base-100 min-w-[33%] flex-nowrap overflow-y-auto">
  <header class="prose flex flex-col items-end p-4 mb-4 max-w-full">
    <h2 class="mb-0">{user.name}</h2>
    <span class="font-light">{user.email ?? user.username}</span>
  </header>

  <li>
    <a href="/logout" data-sveltekit-reload>
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
    <a href="/user" data-sveltekit-preload-data="tap">
      {$t('account_settings.title')}
      <AuthenticatedUserIcon />
    </a>
  </li>

  <div class="divider" />
  <div class="grow" />
  <div class="flex flex-col items-end gap-1">
    <Badge>Client Version: {APP_VERSION}</Badge>
    <Badge>Server Version: {serverVersion}</Badge>
    <Badge>API Version: {apiVersion}</Badge>
  </div>
</ul>

<style>
  a {
    justify-content: flex-end;
  }
</style>
