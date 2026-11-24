<script lang="ts" generics="T">
  import {Badge} from '$lib/components/ui/badge';
  import DiffShell from './DiffShell.svelte';
  import DiffMarker from './DiffMarker.svelte';
  import {diffAdded, diffEmpty, diffKept, diffRemoved, diffTouched} from './diff-classes';

  // Diff counterpart of the editor's EntryOrSenseItemList (complex forms and components).
  // `getKey` must identify the link, not the linked entry: one entry can be linked twice through different senses.
  // `touched` marks a kept link this change references, e.g. a reordered component.
  let {before = [], after = [], getKey, getHeadword, touched}: {
    before?: readonly T[];
    after?: readonly T[];
    getKey: (item: T) => string;
    getHeadword: (item: T) => string | undefined;
    touched?: (item: T) => boolean;
  } = $props();

  const beforeKeys = $derived(new Set(before.map(getKey)));
  const afterKeys = $derived(new Set(after.map(getKey)));
  const kept = $derived(after.filter((item) => beforeKeys.has(getKey(item))));
  const removed = $derived(before.filter((item) => !afterKeys.has(getKey(item))));
  const added = $derived(after.filter((item) => !beforeKeys.has(getKey(item))));

  function label(item: T): string {
    return getHeadword(item) || '–';
  }
</script>

<DiffShell class="flex-wrap items-center gap-1">
  {#each kept as item (getKey(item))}
    <Badge class="text-sm {touched?.(item) ? diffTouched : diffKept}">{label(item)}</Badge>
  {/each}
  {#each removed as item (getKey(item))}
    <Badge class="text-sm {diffRemoved}"><DiffMarker state="removed" />{label(item)}</Badge>
  {/each}
  {#each added as item (getKey(item))}
    <Badge class="text-sm {diffAdded}"><DiffMarker state="added" />{label(item)}</Badge>
  {/each}
  {#if !before.length && !after.length}
    <span class={diffEmpty}>&nbsp;</span>
  {/if}
</DiffShell>
