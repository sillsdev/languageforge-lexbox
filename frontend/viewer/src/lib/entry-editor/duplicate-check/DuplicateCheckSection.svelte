<!-- Keep this fragment-rooted (no wrapping element): the pill's `sticky bottom-0` is clamped to
  its parent's box, which must stay the tall dialog-content column. A wrapper here — or around the
  component at the call site — would collapse its travel room and the pill would stop floating. -->
<script lang="ts">
  import type {IEntry, ISense} from '$lib/dotnet-types';
  import {tick} from 'svelte';
  import DuplicateCheck, {type DuplicateSummary} from './DuplicateCheck.svelte';
  import DuplicateSummaryPill from './DuplicateSummaryPill.svelte';
  import {fly} from 'svelte/transition';

  interface Props {
    entry: IEntry;
    sense?: ISense;
    /** Called right before navigating to an existing entry, so the host dialog can close itself. */
    onNavigateToEntry?: (entry: IEntry) => void;
  }

  let {entry, sense, onNavigateToEntry}: Props = $props();

  let duplicateWidgetEl = $state<HTMLElement>();
  let duplicateCheck = $state<DuplicateCheck>();
  let duplicateSummary = $state<DuplicateSummary>();

  // Expand first: the widget sits near the end of the scrollable content, so without the
  // expanded list below it there isn't enough scroll room to bring its top up the dialog.
  async function jumpToDuplicates(): Promise<void> {
    duplicateCheck?.expand();
    await tick();
    duplicateWidgetEl?.scrollIntoView({behavior: 'smooth', block: 'start'});
  }

  let duplicateWidgetVisible = $state(true);
  let pillDismissed = $state(false);
  $effect(() => {
    const el = duplicateWidgetEl;
    if (!el) return;
    const observer = new IntersectionObserver(
      ([intersection]) => duplicateWidgetVisible = intersection.isIntersecting,
      {root: el.closest('[data-slot="dialog-content"]')});
    observer.observe(el);
    return () => observer.disconnect();
  });
</script>

<div class="mt-3 scroll-mt-2" bind:this={duplicateWidgetEl}>
  <DuplicateCheck {entry} {sense} bind:this={duplicateCheck} bind:summary={duplicateSummary}
    {onNavigateToEntry} />
</div>
{#if duplicateSummary && !duplicateWidgetVisible && !pillDismissed}
  <div class="sticky bottom-0 z-20 h-0 pointer-events-none" transition:fly={{y: 50}}>
    <div class="absolute bottom-3 inset-x-0 flex justify-center">
      <DuplicateSummaryPill summary={duplicateSummary}
        onJump={() => jumpToDuplicates()}
        onDismiss={() => pillDismissed = true} />
    </div>
  </div>
{/if}
