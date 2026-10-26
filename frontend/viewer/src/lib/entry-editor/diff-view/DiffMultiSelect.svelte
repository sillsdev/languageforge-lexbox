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
    const beforeItems = uniqueById(before);
    const afterItems = uniqueById(after);
    const beforeIds = new Set(beforeItems.map(idSelector));
    const afterIds = new Set(afterItems.map(idSelector));
    const merged = [
      ...afterItems.map((item): Item => ({item, state: beforeIds.has(idSelector(item)) ? 'kept' : 'added'})),
      ...beforeItems.filter((item) => !afterIds.has(idSelector(item))).map((item): Item => ({item, state: 'removed'})),
    ];
    // Numeric, so semantic domain 2.1 sorts before 10.1
    return sortValuesBy === 'label' ? merged.sort((a, b) => label(a.item).localeCompare(label(b.item), undefined, {numeric: true})) : merged;
  });

  // A duplicate id would crash the keyed each
  function uniqueById(list: readonly T[] | null | undefined): T[] {
    return [...new Map((list ?? []).map((item) => [idSelector(item), item])).values()];
  }

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
