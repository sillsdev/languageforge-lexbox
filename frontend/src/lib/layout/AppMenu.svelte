<script lang="ts">
  import t from '$lib/i18n';
  import {AdminIcon, AuthenticatedUserIcon, HomeIcon, LogoutIcon} from '$lib/icons';
  import AdminContent from './AdminContent.svelte';
  import Badge from '$lib/components/Badges/Badge.svelte';
  import {APP_VERSION} from '$lib/util/version';
  import type {LexAuthUser} from '$lib/user';
  import Icon from '$lib/icons/Icon.svelte';
  import { helpLinks } from '$lib/components/help';

  interface Props {
    serverVersion: string;
    apiVersion: string | null;
    user: LexAuthUser;
  }

  const { serverVersion, apiVersion, user }: Props = $props();
</script>

<!-- https://daisyui.com/components/menu  -->
<ul class="menu bg-base-100 min-w-[33%] flex-nowrap overflow-y-auto">
  <header class="prose flex flex-col items-end p-4 mb-4 max-w-full">
    <h2 class="mb-0">{user.name}</h2>
    <span class="font-light">{user.emailOrUsername}</span>
  </header>

  <li>
    <a href="/logout" data-sveltekit-reload>
      {$t('appmenu.log_out')}
      <LogoutIcon />
    </a>
  </li>

  <div class="divider"></div>

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
    <a href="/org/list" data-sveltekit-preload-data="tap">
      {$t('appmenu.orgs')}
      <Icon icon="i-mdi-account-group-outline" size="text-2xl" />
    </a>
  </li>

  <li>
    <a href="/user" data-sveltekit-preload-data="tap">
      {$t('account_settings.title')}
      <AuthenticatedUserIcon />
    </a>
  </li>

  <li>
    <a href={helpLinks.helpList} target="_blank" rel="external">
      {$t('appmenu.help')}
      <Icon icon="i-mdi-open-in-new" size="text-2xl" />
    </a>
  </li>

  <div class="divider"></div>
  <div class="grow"></div>
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

  .menu {
    scrollbar-gutter: stable;
  }
</style>
