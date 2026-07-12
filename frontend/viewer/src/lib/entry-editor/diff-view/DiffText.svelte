<script lang="ts">
  import {computeDiff, hasChanges} from './inline-diff';
  import {diffAdded, diffEmpty, diffRemoved} from './diff-classes';

  let {before, after}: {before?: string; after?: string} = $props();

  const segments = $derived(computeDiff(before ?? '', after ?? ''));
  const changed = $derived(hasChanges(segments));
</script>

{#if !before && !after}
  <!-- Blank when empty on both sides (matches the real editor's readonly convention — the DiffShell
       frame around the value is what keeps the row discoverable). -->
  <span class={diffEmpty}>&nbsp;</span>
{:else if !changed}
  <span class="whitespace-pre-wrap break-words text-foreground">{after}</span>
{:else}
  <span class="whitespace-pre-wrap break-words">
    {#each segments as segment, i (i)}
      {#if segment.type === 'added'}
        <span class="rounded-sm {diffAdded}">{segment.value}</span>
      {:else if segment.type === 'removed'}
        <span class="rounded-sm {diffRemoved}">{segment.value}</span>
      {:else}
        <span>{segment.value}</span>
      {/if}
    {/each}
  </span>
{/if}
