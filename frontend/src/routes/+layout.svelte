<script lang="ts">
  import { page } from '$app/stores';
  import '$lib/app.postcss';
  import { error, goesToErrorPage } from '$lib/error';
  import UnexpectedErrorAlert from '$lib/error/UnexpectedErrorAlert.svelte';
  import { onDestroy } from 'svelte';
  import type { LayoutData } from './$types';
  import Notify from '$lib/notify/Notify.svelte';

  onDestroy(
    page.subscribe((p) => {
      error.set(p.error);
    })
  );

  // https://www.w3.org/TR/trace-context/#traceparent-header
  // so the page-load instrumentation can be correlated with the server load
  export let data: LayoutData;
</script>

<svelte:head>
  {#if data.traceParent}
    <meta name="traceparent" content={data.traceParent} />
  {/if}
</svelte:head>

<!-- https://daisyui.com/components/drawer -->
<div>
  <slot />
</div>

<!-- We don't want the alert as well if we're heading to +error.svelte -->
{#if !goesToErrorPage($page.error)}
  <UnexpectedErrorAlert />
{/if}

<Notify />
