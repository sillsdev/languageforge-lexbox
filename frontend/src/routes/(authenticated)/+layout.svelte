<script lang="ts">
  import EmailVerificationStatus from '$lib/email/EmailVerificationStatus.svelte';
  import t from '$lib/i18n';
  import { AdminIcon, AuthenticatedUserIcon, HomeIcon } from '$lib/icons';
  import { AdminContent, AppBar, AppMenu, Breadcrumbs, Content } from '$lib/layout';
  import type { LayoutData } from './$types';
  import { onMount } from 'svelte';
  import { ensureClientMatchesUser } from '$lib/gql';
  import { slide, fly, scale } from 'svelte/transition';
  import NavBalls from '$lib/layout/NavBalls.svelte';

  let menuToggle = false;
  export let data: LayoutData;
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
    ensureClientMatchesUser(user);
  });
</script>

<svelte:window on:keydown={closeOnEscape} />

<div class="drawer drawer-end">
  <input type="checkbox" checked={menuToggle} class="drawer-toggle" />

  <div class="drawer-content">
    <AppBar on:menuopen={open} />
    <div class="bg-neutral text-neutral-content p-2 px-6 grid grid-cols-3 items-center">
      <Breadcrumbs />
      <div class="justify-self-center">
        <NavBalls />
      </div>
      <div class="justify-self-end">
        <NavBalls />
      </div>
    </div>

    <div class="max-w-prose mx-auto mt-6">
      <EmailVerificationStatus {user} />
    </div>

    <Content>
      <slot />
    </Content>
  </div>

  <AppMenu on:click={close} on:keydown={close} {user} serverVersion={data.serverVersion} apiVersion={data.apiVersion} />
</div>
