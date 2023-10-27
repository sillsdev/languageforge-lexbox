<script lang="ts">
  import { getStores } from '$app/stores';
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
  import { onMount } from 'svelte';
  import { blur } from 'svelte/transition';

  export let data: LayoutData;
  const { page, updated } = getStores();

  const error = initErrorStore(writable($page.error));
  $: $error = $page.error;
  $: {
    if (browser && $updated) {
      notifyWarning($t('notifications.update_detected'), Duration.Long);
    }
  }

  let unhydrated = true;
  onMount(() => unhydrated = false);
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

{#if unhydrated}
  <div class="fixed top-0 bottom-0 left-0 right-0 z-10 flex flex-col items-center justify-center" out:blur={{duration: 1000}}>
    <span class="loading loading-spinner bg-primary w-24 z-10"></span>
    <div class="absolute top-0 bottom-0 left-0 right-0 bg-base-100 opacity-60"></div>
  </div>
{/if}

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
