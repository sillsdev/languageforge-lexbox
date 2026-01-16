<script lang="ts">
  import type {Snippet} from 'svelte';
  import EmailVerificationStatus, {
    initEmailResult,
    initRequestedEmail,
  } from '$lib/email/EmailVerificationStatus.svelte';
  import t from '$lib/i18n';
  import {AdminIcon, HomeIcon, Icon} from '$lib/icons';
  import {AdminContent, AppBar, AppMenu, Breadcrumbs, Content} from '$lib/layout';
  import {onMount} from 'svelte';
  import {ensureClientMatchesUser} from '$lib/gql';
  import {beforeNavigate} from '$app/navigation';
  import {resolve} from '$app/paths';
  import {page} from '$app/state';
  import type {LayoutData} from '../../routes/$types';
  import {helpLinks} from '$lib/components/help';

  interface Props {
    hideToolbar?: boolean;
    children?: Snippet;
  }

  const { hideToolbar = false, children }: Props = $props();

  let menuToggle = $state(false);
  let data = $derived(page.data as LayoutData);
  let user = $derived(data.user);

  function close(): void {
    menuToggle = false;
  }

  function closeOnEscape(event: KeyboardEvent): void {
    if (event.key === 'Escape') close();
  }
  onMount(() => {
    if (user) ensureClientMatchesUser(user);
  });
  beforeNavigate(() => close());

  initRequestedEmail();
  initEmailResult();
</script>

<svelte:window onkeydown={closeOnEscape} />

{#if user?.audience === 'LexboxApi' || user?.scope?.includes('lexboxapi')}
  <div class="drawer drawer-end grow">
    <input id="drawer-toggle" type="checkbox" bind:checked={menuToggle} class="drawer-toggle" />

    <div class="drawer-content max-w-[100vw] flex flex-col">
      <AppBar {user} />
      {#if !hideToolbar}
        <div class="bg-neutral text-neutral-content p-2 md:px-6 flex justify-between items-center gap-2">
          <Breadcrumbs />
          <div class="flex gap-2 items-center">
            <!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
            <a href={helpLinks.helpList}
              target="_blank"
              rel="external"
              class="btn btn-sm btn-info btn-outline hidden lg:flex"
            >
              {$t('appmenu.help')}
              <Icon icon="i-mdi-open-in-new" size="text-lg" />
            </a>
            <a href={resolve('/org/list')} class="btn btn-sm btn-secondary">
              <span class="max-md:hidden">
                {$t('appmenu.orgs')}
              </span>
              <Icon icon="i-mdi-account-group-outline" size="text-xl" />
            </a>
            <a href={resolve('/')} class="btn btn-sm btn-primary">
              <span class="max-md:hidden">
                {$t('user_dashboard.title')}
              </span>
              <HomeIcon size="text-xl" />
            </a>
            <AdminContent>
              <a href={resolve('/admin')} class="btn btn-sm btn-accent">
                <span class="max-md:hidden">
                  {$t('admin_dashboard.title')}
                </span>
                <AdminIcon size="text-xl" />
              </a>
            </AdminContent>
          </div>
        </div>
      {/if}

      <div class="max-w-prose mx-auto email-status-container">
        <EmailVerificationStatus {user} />
      </div>

      <Content>
        {@render children?.()}
      </Content>
    </div>
    <div class="drawer-side z-10">
      <!-- using a label means it works before hydration is complete -->
      <label for="drawer-toggle" class="drawer-overlay"></label>
      <AppMenu {user} serverVersion={data.serverVersion} apiVersion={data.apiVersion} />
    </div>
  </div>
{:else}
  <!-- If the user is authenticated (i.e. not null), but not authorized to use the API, we
  still want to pass them in here, so we show their name rather than Login and Register buttons. -->
  <AppBar {user} />

  <Content>
    {@render children?.()}
  </Content>
{/if}

<style lang="postcss">
  :global(.email-status-container > div:first-child) {
    @apply mt-6;
  }
</style>
