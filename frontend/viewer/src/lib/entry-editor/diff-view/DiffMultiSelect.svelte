<script lang="ts" generics="T">
  import {Badge} from '$lib/components/ui/badge';
  import DiffShell from './DiffShell.svelte';
  import {diffAdded, diffEmpty, diffKept, diffRemoved} from './diff-classes';

  let {before = [], after = [], idSelector, labelSelector, sortKey}: {
    before?: readonly T[];
    after?: readonly T[];
    idSelector: (item: T) => string;
    labelSelector: (item: T) => string;
    // How to order the merged list. Defaults to the label (semantic-domain labels are code-prefixed, so
    // this already sorts by code). Pass an explicit key when the display label isn't the sort field.
    sortKey?: (item: T) => string;
  } = $props();

  const beforeIds = $derived(new Set(before.map(idSelector)));
  const afterIds = $derived(new Set(after.map(idSelector)));

  // Merge before + after into one list (deduped by id) and sort it, so a removed item keeps its sorted
  // position rather than being dumped after the kept ones. Each entry is tagged with its diff state.
  const key = $derived(sortKey ?? labelSelector);
  const merged = $derived.by(() => {
    type Entry = {item: T; state: 'kept' | 'removed' | 'added'};
    const fromBefore: Entry[] = before.map((item) => ({item, state: afterIds.has(idSelector(item)) ? 'kept' : 'removed'}));
    const fromAfter: Entry[] = after.filter((item) => !beforeIds.has(idSelector(item))).map((item) => ({item, state: 'added'}));
    return [...fromBefore, ...fromAfter].sort((a, b) => key(a.item).localeCompare(key(b.item)));
  });

  const stateClass = {kept: diffKept, removed: diffRemoved, added: diffAdded} as const;
</script>

{#if !before.length && !after.length}
  <DiffShell class="items-center">
    <span class={diffEmpty}>&nbsp;</span>
  </DiffShell>
{:else}
  <DiffShell class="flex-wrap items-center gap-1">
    {#each merged as {item, state} (idSelector(item))}
      <Badge class={stateClass[state]}>{labelSelector(item)}</Badge>
    {/each}
  </DiffShell>
{/if}
