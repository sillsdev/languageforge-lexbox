<script lang="ts">
  import EmailVerificationStatus from '$lib/email/EmailVerificationStatus.svelte';
  import t from '$lib/i18n';
  import { AdminIcon } from '$lib/icons';
  import { AdminContent, AppBar, AppMenu, Breadcrumbs, Content } from '$lib/layout';
  import { onMount } from 'svelte';
  import { ensureClientMatchesUser } from '$lib/gql';
  import { beforeNavigate } from '$app/navigation';
  import { page } from '$app/stores';
  import type { LayoutData } from '../../routes/$types';

  let menuToggle = false;
  $: data = $page.data as LayoutData;
  $: user = data.user;

  function open(): void {
    menuToggle = true;
  }

  function close(): void {
    menuToggle = false;
  }

  function closeOnEscape(event: KeyboardEvent): void {
    event.key === 'Escape' && close();
  }
  onMount(() => {
    if (user) ensureClientMatchesUser(user);
  });
  beforeNavigate(() => close());
</script>

<svelte:window on:keydown={closeOnEscape} />

{#if user}
  <div class="drawer drawer-end">
    <input type="checkbox" checked={menuToggle} class="drawer-toggle" />

    <div class="drawer-content max-w-[100vw]">
      <AppBar on:menuopen={open} {user} />
      <div class="bg-neutral text-neutral-content p-2 md:px-6 flex justify-between items-center">
        <Breadcrumbs />
        <AdminContent>
          <a href="/admin" class="btn btn-sm btn-accent">
            <span class="max-sm:hidden">
              {$t('admin_dashboard.title')}
            </span>
            <AdminIcon />
          </a>
        </AdminContent>
      </div>

      <div class="max-w-prose mx-auto email-status-container">
        <EmailVerificationStatus {user} />
      </div>

      <Content>
        <slot />
      </Content>
    </div>
    <div class="drawer-side z-10">
      <button class="drawer-overlay" on:click={close} on:keydown={close} />
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
