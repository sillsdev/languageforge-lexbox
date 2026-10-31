<script lang="ts">
  import {computeDiff, hasChanges} from './inline-diff';
  import {diffAdded, diffEmpty, diffRemoved} from './diff-classes';
  import DiffMarker from './DiffMarker.svelte';

  let {before, after}: {before?: string; after?: string} = $props();

  const segments = $derived(computeDiff(before ?? '', after ?? ''));
</script>

{#if !before && !after}
  <span class={diffEmpty}>&nbsp;</span>
{:else if !hasChanges(segments)}
  <span class="whitespace-pre-wrap break-words text-foreground">{after}</span>
{:else}
  <span class="whitespace-pre-wrap break-words">
    {#each segments as {type, value}, i (i)}
      {#if type === 'equal'}
        <span>{value}</span>
      {:else}
        <span class="rounded-sm {type === 'added' ? diffAdded : diffRemoved}"><DiffMarker state={type} />{value}</span>
      {/if}
    {/each}
  </span>
{/if}
