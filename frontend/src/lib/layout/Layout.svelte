<script lang="ts">
  import EmailVerificationStatus, { initEmailResult, initRequestedEmail } from '$lib/email/EmailVerificationStatus.svelte';
  import t from '$lib/i18n';
  import { AdminIcon, Icon } from '$lib/icons';
  import { AdminContent, AppBar, AppMenu, Breadcrumbs, Content } from '$lib/layout';
  import { onMount } from 'svelte';
  import { ensureClientMatchesUser } from '$lib/gql';
  import { beforeNavigate } from '$app/navigation';
  import { page } from '$app/stores';
  import type { LayoutData } from '../../routes/$types';
  import DevContent from './DevContent.svelte';

  let menuToggle = false;
  $: data = $page.data as LayoutData;
  $: user = data.user;

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

<svelte:window on:keydown={closeOnEscape} />

{#if user}
  <div class="drawer drawer-end">
    <input id="drawer-toggle" type="checkbox" bind:checked={menuToggle} class="drawer-toggle" />

    <div class="drawer-content max-w-[100vw]">
      <AppBar {user} />
      <div class="bg-neutral text-neutral-content p-2 md:px-6 flex justify-between items-center gap-2">
        <Breadcrumbs />
        <div class="flex gap-4 items-center">
          <DevContent>
            <a href="/sandbox" class="btn btn-sm btn-secondary">
              <span class="max-sm:hidden">
                Sandbox
              </span>
              <Icon size="text-2xl" icon="i-mdi-box-variant" />
            </a>
          </DevContent>
          <AdminContent>
            <a href="/admin" class="btn btn-sm btn-accent">
              <span class="max-sm:hidden">
                {$t('admin_dashboard.title')}
              </span>
              <AdminIcon />
            </a>
          </AdminContent>
        </div>
      </div>

      <div class="max-w-prose mx-auto email-status-container">
        <EmailVerificationStatus {user} />
      </div>

      <Content>
        <slot />
      </Content>
    </div>
    <div class="drawer-side z-10">
      <!-- using a label means it works before hydration is complete -->
      <label for="drawer-toggle" class="drawer-overlay" />
      <AppMenu {user} serverVersion={data.serverVersion} apiVersion={data.apiVersion} />
    </div>
  </div>
{:else}
  <AppBar user={undefined} />

  <Content>
    <slot />
  </Content>
{/if}

<style lang="postcss">
  :global(.email-status-container > div:first-child) {
    @apply mt-6;
  }
</style>
