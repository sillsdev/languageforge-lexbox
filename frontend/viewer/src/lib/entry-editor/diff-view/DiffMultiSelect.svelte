<script lang="ts" generics="T">
  import {Badge} from '$lib/components/ui/badge';
  import DiffShell from './DiffShell.svelte';
  import {diffAdded, diffEmpty, diffKept, diffRemoved} from './diff-classes';

  let {before = [], after = [], idSelector, labelSelector}: {
    before?: readonly T[];
    after?: readonly T[];
    idSelector: (item: T) => string;
    labelSelector: (item: T) => string;
  } = $props();

  const beforeIds = $derived(new Set(before.map(idSelector)));
  const afterIds = $derived(new Set(after.map(idSelector)));
  const kept = $derived(after.filter((item) => beforeIds.has(idSelector(item))));
  const removed = $derived(before.filter((item) => !afterIds.has(idSelector(item))));
  const added = $derived(after.filter((item) => !beforeIds.has(idSelector(item))));
</script>

{#if !before.length && !after.length}
  <DiffShell class="items-center">
    <span class={diffEmpty}>&nbsp;</span>
  </DiffShell>
{:else}
  <DiffShell class="flex-wrap items-center gap-1">
    {#each kept as item (idSelector(item))}
      <Badge class={diffKept}>{labelSelector(item)}</Badge>
    {/each}
    {#each removed as item (idSelector(item))}
      <Badge class={diffRemoved}>{labelSelector(item)}</Badge>
    {/each}
    {#each added as item (idSelector(item))}
      <Badge class={diffAdded}>{labelSelector(item)}</Badge>
    {/each}
  </DiffShell>
{/if}
