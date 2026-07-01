<script lang="ts">
  import DiffShell from './DiffShell.svelte';
  import {diffAdded, diffEmpty, diffRemoved} from './diff-classes';

  let {before, after}: {before?: string; after?: string} = $props();

  const changed = $derived((before ?? '') !== (after ?? ''));
</script>

<DiffShell class="items-center">
  {#if !before && !after}
    <span class={diffEmpty}>&nbsp;</span>
  {:else if !changed}
    <span class="text-foreground">{after}</span>
  {:else}
    <span class="inline-flex flex-wrap items-center gap-1.5">
      {#if before}
        <span class="rounded-sm px-1 {diffRemoved}">{before}</span>
      {/if}
      {#if before && after}
        <span class="text-muted-foreground">→</span>
      {/if}
      {#if after}
        <span class="rounded-sm px-1 {diffAdded}">{after}</span>
      {/if}
    </span>
  {/if}
</DiffShell>
