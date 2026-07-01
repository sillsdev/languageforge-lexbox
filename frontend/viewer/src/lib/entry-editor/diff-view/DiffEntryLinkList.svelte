<script lang="ts" generics="T">
  import {t} from 'svelte-i18n-lingui';
  import {Badge} from '$lib/components/ui/badge';
  import DiffShell from './DiffShell.svelte';
  import {diffAdded, diffEmpty, diffKept, diffRemoved, diffTouched} from './diff-classes';

  // Diff counterpart of the editor's `EntryOrSenseItemList` (used by ComplexFormComponents and ComplexForms).
  // Uses the same shared palette as every other diff leaf so add/remove/kept read the same across the app.
  // `getEntryId` scopes diff-set membership (an entry is "kept" if the linked-entry id survives).
  // `getKey` is only for {#each} keying; defaults to getEntryId but callers should override with a
  // truly unique per-item key (e.g. the CFC's own id) when the same linked-entry could appear twice.
  // `touched` marks a still-present item that this specific change references (e.g. a reordered CFC).
  let {before = [], after = [], getEntryId, getHeadword, getKey, touched}: {
    before?: readonly T[];
    after?: readonly T[];
    getEntryId: (item: T) => string;
    getHeadword: (item: T) => string | undefined;
    getKey?: (item: T) => string;
    touched?: (item: T) => boolean;
  } = $props();

  const beforeIds = $derived(new Set(before.map(getEntryId)));
  const afterIds = $derived(new Set(after.map(getEntryId)));
  const kept = $derived(after.filter((item) => beforeIds.has(getEntryId(item))));
  const removed = $derived(before.filter((item) => !afterIds.has(getEntryId(item))));
  const added = $derived(after.filter((item) => !beforeIds.has(getEntryId(item))));

  function label(item: T): string {
    return getHeadword(item) || $t`Untitled`;
  }
  function key(item: T): string {
    return getKey ? getKey(item) : getEntryId(item);
  }
</script>

{#if !before.length && !after.length}
  <DiffShell class="items-center">
    <span class={diffEmpty}>&nbsp;</span>
  </DiffShell>
{:else}
  <DiffShell class="flex-wrap items-center gap-1">
    {#each kept as item (key(item))}
      <Badge class={touched?.(item) ? diffTouched : diffKept}>{label(item)}</Badge>
    {/each}
    {#each removed as item (key(item))}
      <Badge class={diffRemoved}>{label(item)}</Badge>
    {/each}
    {#each added as item (key(item))}
      <Badge class={diffAdded}>{label(item)}</Badge>
    {/each}
  </DiffShell>
{/if}
