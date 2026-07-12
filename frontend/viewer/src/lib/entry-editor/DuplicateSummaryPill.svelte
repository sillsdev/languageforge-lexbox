<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Badge} from '$lib/components/ui/badge';
  import {Icon} from '$lib/components/ui/icon';
  import {duplicateTintClass, trapEnter} from './duplicate-check';
  import type {DuplicateSummary} from './DuplicateCheck.svelte';

  interface Props {
    summary: DuplicateSummary;
    /** Called when the pill body is activated — the host scrolls the duplicate widget into view. */
    onJump: () => void;
    onDismiss: () => void;
  }

  let {summary, onJump, onDismiss}: Props = $props();
</script>

<!-- One visual pill, two sibling buttons (a button can't nest a button).
  mousedown preventDefault: focusing the pill makes Chromium cancel the smooth scroll it triggers -->
<!-- opaque bg-background underlay: the tint colors are translucent washes shared with
  the duplicate widget's trigger, and the pill floats over form content -->
<!-- 32rem cap: the headword preview would otherwise stretch the pill across the whole dialog -->
<div class="pointer-events-auto max-w-[min(100%,32rem)] rounded-full bg-background shadow-md">
  <div class="max-w-full flex items-center rounded-full border text-sm {duplicateTintClass(summary.hasExactWordMatch)}">
    <button
      type="button"
      aria-label={summary.message}
      class="min-w-0 flex items-center gap-2 rounded-s-full ps-3 py-1.5 relative after:absolute after:content-[''] after:-inset-y-2.5 after:-start-2.5 after:end-0"
      onkeydown={trapEnter}
      onmousedown={e => e.preventDefault()}
      onclick={onJump}>
      {#if summary.hasExactWordMatch}
        <Icon icon="i-mdi-alert-circle-outline" class="size-4 shrink-0 text-amber-600 dark:text-amber-400" />
      {:else}
        <Icon icon="i-mdi-information-outline" class="size-4 shrink-0 text-muted-foreground" />
      {/if}
      <span class="min-w-0 truncate font-medium">
        {summary.message}
        {#if summary.previewHeadwords}
          <span class="text-muted-foreground font-normal">— {summary.previewHeadwords}</span>
        {/if}
      </span>
      <Badge variant="secondary">{summary.count}{summary.capped ? '+' : ''}</Badge>
      <Icon icon="i-mdi-chevron-down" class="size-4 shrink-0" />
    </button>
    <button
      type="button"
      aria-label={$t`Close`}
      class="flex items-center rounded-e-full ps-1.5 pe-2.5 py-1.5 self-stretch relative after:absolute after:content-[''] after:-inset-y-2.5 after:start-0 after:-end-2.5"
      onkeydown={trapEnter}
      onmousedown={e => e.preventDefault()}
      onclick={onDismiss}>
      <Icon icon="i-mdi-close" class="size-4 shrink-0" />
    </button>
  </div>
</div>
