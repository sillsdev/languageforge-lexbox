<script lang="ts">
  import EmailVerificationStatus from '$lib/email/EmailVerificationStatus.svelte';
  import t from '$lib/i18n';
  import { AdminIcon } from '$lib/icons';
  import { AdminContent, AppBar, AppMenu, Breadcrumbs, Content } from '$lib/layout';
  import type { LayoutData } from './$types';

  let menuToggle = false;
  export let data: LayoutData;
  const user = data.user;

  function open(): void {
    menuToggle = true;
  }

  function close(): void {
    menuToggle = false;
  }

  function closeOnEscape(event: KeyboardEvent): void {
    event.key === 'Escape' && close();
  }
</script>

<svelte:window on:keydown={closeOnEscape} />

<div class="drawer drawer-end">
  <input type="checkbox" checked={menuToggle} class="drawer-toggle" />

  <div class="drawer-content">
    <AppBar on:menuopen={open} />
    <div class="bg-neutral text-neutral-content p-2 pl-6 flex justify-between items-center">
      <Breadcrumbs />
      <AdminContent>
        <a href="/admin" class="btn btn-sm btn-accent hidden sm:inline-flex">
          {$t('admin_dashboard.title')}
          <span class="ml-2"><AdminIcon /></span>
        </a>
      </AdminContent>
    </div>

    {#if !user.emailVerified}
      <div class="max-w-prose mx-auto mt-6">
        <EmailVerificationStatus {user} />
      </div>
    {/if}

    <Content>
      <slot />
    </Content>
  </div>

  <AppMenu on:click={close} on:keydown={close} {user} serverVersion={data.serverVersion} apiVersion={data.apiVersion} />
</div>
