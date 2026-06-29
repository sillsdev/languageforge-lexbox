<script lang="ts">
  import DiffShell from './DiffShell.svelte';

  let {before, after}: {before?: string; after?: string} = $props();

  const changed = $derived((before ?? '') !== (after ?? ''));
</script>

<DiffShell class="items-center">
  {#if !before && !after}
    <span class="text-muted-foreground/50 italic">—</span>
  {:else if !changed}
    <span class="text-muted-foreground">{after}</span>
  {:else}
    <span class="inline-flex flex-wrap items-center gap-1.5">
      {#if before}
        <span class="rounded-sm bg-destructive/10 px-1 text-destructive line-through decoration-destructive/50">{before}</span>
      {/if}
      <span class="text-muted-foreground">→</span>
      {#if after}
        <span class="rounded-sm bg-emerald-500/15 px-1 text-emerald-700 dark:text-emerald-300">{after}</span>
      {/if}
    </span>
  {/if}
</DiffShell>
