<script lang="ts">
  import { page, updated } from '$app/stores';
  import '$lib/app.postcss';
  import { initErrorStore } from '$lib/error';
  import UnexpectedErrorAlert from '$lib/error/UnexpectedErrorAlert.svelte';
  import type { LayoutData } from './$types';
  import Notify from '$lib/notify/Notify.svelte';
  import { Footer } from '$lib/layout';
  import { writable } from 'svelte/store';
  import { notifyWarning } from '$lib/notify';
  import { Duration } from '$lib/util/time';
  import { browser } from '$app/environment';
  import t from '$lib/i18n';

  export let data: LayoutData;

  const error = initErrorStore(writable($page.error));
  $: $error = $page.error;
  $: {
    if (browser && $updated) {
      notifyWarning($t('notifications.update_detected'), Duration.Long);
    }
  }
</script>

<svelte:head>
  {#if data.traceParent}
    <!--
      https://www.w3.org/TR/trace-context/#traceparent-header
      so the page-load instrumentation can be correlated with the server load
    -->
    <meta name="traceparent" content={data.traceParent} />
  {/if}
</svelte:head>

<div class="flex flex-col justify-between min-h-full">
  <div class="flex flex-col flex-grow">
    <slot />
  </div>
  <Footer />
</div>

<!-- We don't want the alert as well if we're heading to +error.svelte -->
{#if !$page.error}
  <UnexpectedErrorAlert />
{/if}

<Notify />
