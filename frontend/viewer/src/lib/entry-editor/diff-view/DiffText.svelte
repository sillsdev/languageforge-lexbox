<script lang="ts">
  import {computeDiff, hasChanges} from './inline-diff';

  let {before, after}: {before?: string; after?: string} = $props();

  const segments = $derived(computeDiff(before ?? '', after ?? ''));
  const changed = $derived(hasChanges(segments));
</script>

{#if !before && !after}
  <span class="text-muted-foreground/50 italic">—</span>
{:else if !changed}
  <span class="whitespace-pre-wrap break-words text-muted-foreground">{after}</span>
{:else}
  <span class="whitespace-pre-wrap break-words">
    {#each segments as segment, i (i)}
      {#if segment.type === 'added'}
        <span class="rounded-sm bg-emerald-500/15 text-emerald-700 dark:text-emerald-300">{segment.value}</span>
      {:else if segment.type === 'removed'}
        <span class="rounded-sm bg-destructive/10 text-destructive line-through decoration-destructive/50">{segment.value}</span>
      {:else}
        <span>{segment.value}</span>
      {/if}
    {/each}
  </span>
{/if}
