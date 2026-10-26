<script lang="ts" generics="T">
  import {t} from 'svelte-i18n-lingui';
  import {Badge} from '$lib/components/ui/badge';
  import DiffShell from './DiffShell.svelte';
  import DiffMarker from './DiffMarker.svelte';
  import {diffAdded, diffEmpty, diffKept, diffRemoved} from './diff-classes';

  let {before, after, idSelector, labelSelector, sortValuesBy = 'label'}: {
    before?: readonly T[] | null;
    after?: readonly T[] | null;
    idSelector: (item: T) => string;
    labelSelector: (item: T) => string;
    /** Match the editor's MultiSelect: 'selectionOrder' keeps the list's own order, with removed items last. */
    sortValuesBy?: 'label' | 'selectionOrder';
  } = $props();

  type State = 'kept' | 'added' | 'removed';
  type Item = {item: T; state: State};
  const stateClass: Record<State, string> = {kept: diffKept, added: diffAdded, removed: diffRemoved};

  // Kept items come from `after`, so a renamed item shows its current name.
  const items = $derived.by(() => {
    const beforeIds = new Set(before?.map(idSelector));
    const afterIds = new Set(after?.map(idSelector));
    const merged = [
      ...(after ?? []).map((item): Item => ({item, state: beforeIds.has(idSelector(item)) ? 'kept' : 'added'})),
      ...(before ?? []).filter((item) => !afterIds.has(idSelector(item))).map((item): Item => ({item, state: 'removed'})),
    ];
    return sortValuesBy === 'label' ? merged.sort((a, b) => label(a.item).localeCompare(label(b.item))) : merged;
  });

  function label(item: T): string {
    return labelSelector(item) || $t`Untitled`;
  }
</script>

<DiffShell class="flex-wrap items-center gap-1">
  {#each items as {item, state} (idSelector(item))}
    <Badge class="text-sm {stateClass[state]}">
      {#if state !== 'kept'}<DiffMarker {state} />{/if}{label(item)}
    </Badge>
  {:else}
    <span class={diffEmpty}>&nbsp;</span>
  {/each}
</DiffShell>
