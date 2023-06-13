<script lang="ts">
  import { page } from '$app/stores';
  import '$lib/app.postcss';
  import { error } from '$lib/error';
  import UnexpectedErrorAlert from '$lib/error/UnexpectedErrorAlert.svelte';
  import { AppBar, AppMenu } from '$lib/layout';
  import { user } from '$lib/user';
  import { onDestroy } from 'svelte';
  import type { LayoutData } from './$types';

  let menuToggle = false;

  function open(): void {
    menuToggle = true;
  }

  function close(): void {
    menuToggle = false;
  }

  function closeOnEscape(event: KeyboardEvent): void {
    event.key === 'Escape' && close();
  }

  onDestroy(
    page.subscribe((p) => {
      error.set(p.error);
    })
  );

  // https://www.w3.org/TR/trace-context/#traceparent-header
  // so the page-load instrumentation can be correlated with the server load
  export let data: LayoutData;
</script>

<svelte:window on:keydown={closeOnEscape} />

<svelte:head>
  {#if data.traceParent}
    <meta name="traceparent" content={data.traceParent} />
  {/if}
</svelte:head>

<!-- https://daisyui.com/components/drawer -->
<div class="drawer drawer-end">
  <input type="checkbox" checked={menuToggle} class="drawer-toggle" />

  <div class="drawer-content">
    <AppBar on:menuopen={open} />

    <!-- https://tailwindcss.com/docs/typography-plugin -->
    <main class="max-w-none px-2 md:px-6 pt-8">
      <slot />
    </main>
  </div>

  {#if $user}
    <AppMenu on:click={close} on:keydown={close} />
  {/if}
</div>

<!-- We don't want the alert for "hook" errors, because they land on +error.svelte -->
{#if !$page.error?.source?.endsWith('-hook')}
  <UnexpectedErrorAlert />
{/if}
