<script lang="ts" module>
  export type FilterChip = {key: string, label: string, remove: () => void};

  // How many chips fit in maxRows when the overflow chip (if any) and the clear chip must fit too
  function countFitting(chipWidths: number[], moreWidth: number, clearWidth: number, rowWidth: number, maxRows: number, gap: number): number {
    for (let n = chipWidths.length; n > 0; n--) {
      const widths = chipWidths.slice(0, n);
      if (n < chipWidths.length) widths.push(moreWidth);
      widths.push(clearWidth);
      if (rowsNeeded(widths, rowWidth, gap) <= maxRows) return n;
    }
    return 0;
  }

  function rowsNeeded(widths: number[], rowWidth: number, gap: number): number {
    let rows = 1;
    let used = 0;
    for (const w of widths) {
      if (used > 0 && used + gap + w > rowWidth) {
        rows++;
        used = w;
      } else {
        used += (used > 0 ? gap : 0) + w;
      }
    }
    return rows;
  }
</script>

<script lang="ts">
  import {tick} from 'svelte';
  import {t, plural} from 'svelte-i18n-lingui';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';

  let {chips, onShowMore, onClear, onEmptied}: {
    chips: FilterChip[],
    onShowMore: () => void,
    onClear: () => void,
    onEmptied: () => void,
  } = $props();

  const GAP = 6;
  const maxRows = $derived(IsMobile.value ? 1 : 2);

  let rowWidth = $state(0);
  // Changes when fonts load, so the fit is measured again
  let measuredWidth = $state(0);
  let measureChips = $state<HTMLElement[]>([]);
  let measureMore = $state<HTMLElement>();
  let measureClear = $state<HTMLElement>();
  let chipButtons = $state<HTMLButtonElement[]>([]);
  let moreButton = $state<HTMLButtonElement>();
  let visibleCount = $state(Infinity);
  // Space left for a lone chip beside the overflow and clear chips, so at least one filter always shows
  let loneChipMaxWidth = $state<number>();

  $effect(() => {
    if (!rowWidth || !measuredWidth || !measureMore || !measureClear) return;
    // Measured widths are fractional while clientWidth is rounded, so leave a pixel to spare
    const width = rowWidth - 1;
    const widths = chips.map((_, i) => measureChips[i]?.getBoundingClientRect().width ?? 0);
    const moreWidth = measureMore.getBoundingClientRect().width;
    const clearWidth = measureClear.getBoundingClientRect().width;
    const fitting = countFitting(widths, moreWidth, clearWidth, width, maxRows, GAP);
    visibleCount = Math.max(fitting, 1);
    const besideLoneChip = clearWidth + GAP + (chips.length > 1 ? moreWidth + GAP : 0);
    loneChipMaxWidth = fitting === 0 ? width - besideLoneChip : undefined;
  });

  const visibleChips = $derived(chips.slice(0, visibleCount));
  const hiddenCount = $derived(chips.length - visibleChips.length);

  async function remove(index: number) {
    chips[index].remove();
    await tick();
    const next = chipButtons[Math.min(index, visibleChips.length - 1)] ?? moreButton;
    if (next) next.focus();
    else onEmptied();
  }
</script>

{#snippet chipContent(label: string)}
  <span class="truncate">{label}</span>
  <span class="i-mdi-close size-3.5 shrink-0" aria-hidden="true"></span>
{/snippet}

<div class="relative" bind:clientWidth={rowWidth}>
  <!-- Natural chip widths, for deciding how many fit -->
  <div class="invisible absolute h-0 w-max overflow-hidden flex" aria-hidden="true" bind:clientWidth={measuredWidth}>
    {#each chips as chip, i (chip.key)}
      <span class="chip" bind:this={measureChips[i]}>{@render chipContent(chip.label)}</span>
    {/each}
    <span class="chip-plain" bind:this={measureMore}>{$plural(9, {one: '+# more', other: '+# more'})}</span>
    <span class="chip-plain" bind:this={measureClear}>{$t`Clear filters`}</span>
  </div>

  {#if chips.length}
    <div class="flex flex-wrap pt-1.5" style:gap="{GAP}px">
      {#each visibleChips as chip, i (chip.key)}
        <button type="button" class="chip" title={chip.label} aria-label={$t`Remove filter: ${chip.label}`}
          style:max-width={i === 0 && loneChipMaxWidth ? `${loneChipMaxWidth}px` : undefined}
          bind:this={chipButtons[i]} onclick={() => remove(i)}>
          {@render chipContent(chip.label)}
        </button>
      {/each}
      {#if hiddenCount > 0}
        <button type="button" class="chip-plain" bind:this={moreButton} onclick={onShowMore}>
          {$plural(hiddenCount, {one: '+# more', other: '+# more'})}
        </button>
      {/if}
      <button type="button" class="chip-plain" onclick={() => { onClear(); onEmptied(); }}>
        {$t`Clear filters`}
      </button>
    </div>
  {/if}
  <span class="sr-only" role="status">
    {chips.length ? $plural(chips.length, {one: '# filter applied', other: '# filters applied'}) : ''}
  </span>
</div>

<style lang="postcss">
  @reference "#app.css";

  .chip, .chip-plain {
    @apply inline-flex shrink-0 items-center gap-1 rounded-full px-2.5 h-8 md:h-7 text-sm md:text-xs font-medium whitespace-nowrap;
  }

  .chip {
    @apply max-w-[min(12rem,100%)] bg-primary/10 text-primary hover:bg-primary/20;
  }

  .chip-plain {
    @apply border hover:bg-accent;
  }
</style>
