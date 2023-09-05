<script lang="ts">
  import { page } from '$app/stores';
  import '$lib/app.postcss';
  import { goesToErrorPage, initErrorStore } from '$lib/error';
  import UnexpectedErrorAlert from '$lib/error/UnexpectedErrorAlert.svelte';
  import type { LayoutData } from './$types';
  import Notify from '$lib/notify/Notify.svelte';
  import { Footer } from '$lib/layout';
  import { writable } from 'svelte/store';
  import { onDestroy } from 'svelte';

  // https://www.w3.org/TR/trace-context/#traceparent-header
  // so the page-load instrumentation can be correlated with the server load
  export let data: LayoutData;

  const error = initErrorStore(writable());
  onDestroy(
    page.subscribe((p) => {
      error.set(p.error);
    })
  );
</script>

<svelte:head>
  {#if data.traceParent}
    <meta name="traceparent" content={data.traceParent} />
  {/if}
</svelte:head>

<div class="flex flex-col justify-between h-full">
  <div>
    <slot />
  </div>
  <Footer />
</div>

<!-- We don't want the alert as well if we're heading to +error.svelte -->
{#if !goesToErrorPage($page.error)}
  <UnexpectedErrorAlert />
{/if}

<Notify />
