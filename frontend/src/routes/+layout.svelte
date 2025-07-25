<script lang="ts">
  import type { Snippet } from 'svelte';
  import { getStores, navigating } from '$app/stores';
  import '$lib/app.postcss';
  import { initErrorStore } from '$lib/error';
  import UnexpectedErrorAlert from '$lib/error/UnexpectedErrorAlert.svelte';
  import type { LayoutData } from './$types';
  import Notify from '$lib/notify/Notify.svelte';
  import { Footer } from '$lib/layout';
  import { initNotificationService } from '$lib/notify';
  import { overlayContainer } from '$lib/overlay';
  import { Duration } from '$lib/util/time';
  import { browser } from '$app/environment';
  import { onMount, setContext } from 'svelte';
  import { derived, writable } from 'svelte/store';
  import { initI18n } from '$lib/i18n';

  interface Props {
    data: LayoutData;
    children?: Snippet;
  }

  const { data, children }: Props = $props();
  const { page, updated } = getStores();

  const { t, locale } = initI18n(data.activeLocale);
  $effect(() => {
    if (data.activeLocale) locale.set(data.activeLocale);
  });

  const { notifyWarning } = initNotificationService();
  setContext('breadcrumb-store', writable([] as Element[]));

  const error = initErrorStore($page.error);
  $effect(() => {
    $error = $page.error;
  });
  $effect(() => {
    if (browser && $updated) {
      notifyWarning($t('notifications.update_detected'), Duration.Long);
    }
  });

  let hydrating = $state(true);
  onMount(() => (hydrating = false));

  const loading = derived(navigating, (nav) => {
    if (!nav?.to) return false;
    if (!nav.from) return true;
    return nav.to.url.pathname !== nav.from.url.pathname;
  });
</script>

<!-- We don't want the alert as well if we're heading to +error.svelte -->
{#if !$page.error}
  <UnexpectedErrorAlert />
{/if}

<div use:overlayContainer class="bg-base-200 shadow rounded-box z-[2] absolute"></div>

<svelte:head>
  {#if data.traceParent}
    <!--
      https://www.w3.org/TR/trace-context/#traceparent-header
      so the page-load instrumentation can be correlated with the server load
    -->
    <meta name="traceparent" content={data.traceParent} />
  {/if}
</svelte:head>

{#if $loading || hydrating}
  <progress class="progress progress-info block fixed z-50 h-[3px] rounded-none bg-transparent"></progress>
{/if}

<div class="flex flex-col justify-between min-h-full" class:hydrating>
  <div class="flex flex-col flex-grow">
    {@render children?.()}
  </div>
  <Footer />
</div>

<Notify />
