<script lang="ts">
  import type {IPicture} from '$lib/dotnet-types';
  import {t} from 'svelte-i18n-lingui';
  import {cn} from '$lib/utils';
  import PictureImage from '../field-editors/PictureImage.svelte';
  import DiffMarker from './DiffMarker.svelte';
  import {diffAddedSurface, diffRemovedSurface, diffTouched} from './diff-classes';

  let {before = [], after = []}: {before?: readonly IPicture[]; after?: readonly IPicture[]} = $props();

  type Row = {picture: IPicture; state?: 'added' | 'removed' | 'moved'};
  const surface = {added: diffAddedSurface, removed: diffRemovedSurface, moved: diffTouched};

  // A picture whose image or caption changed shows as its old version removed and its new version added.
  const rows = $derived.by(() => {
    const beforeById = new Map(before.map((picture) => [picture.id, picture]));
    const afterIds = new Set(after.map((picture) => picture.id));
    // A reorder only shows as pictures whose place among the kept ones changed
    const keptBefore = before.filter((picture) => afterIds.has(picture.id)).map((picture) => picture.id);
    const keptAfter = after.filter((picture) => beforeById.has(picture.id)).map((picture) => picture.id);
    const rows: Row[] = [];
    for (const picture of after) {
      const old = beforeById.get(picture.id);
      if (old && isSame(old, picture)) {
        const moved = keptBefore.indexOf(picture.id) !== keptAfter.indexOf(picture.id);
        rows.push({picture, state: moved ? 'moved' : undefined});
      } else {
        if (old) rows.push({picture: old, state: 'removed'});
        rows.push({picture, state: 'added'});
      }
    }
    for (const picture of before) {
      if (!afterIds.has(picture.id)) rows.push({picture, state: 'removed'});
    }
    return rows;
  });

  function isSame(a: IPicture, b: IPicture): boolean {
    return a.mediaUri === b.mediaUri && JSON.stringify(a.caption) === JSON.stringify(b.caption);
  }
</script>

{#if rows.length}
  <div class="flex flex-wrap gap-4">
    {#each rows as {picture, state} (`${state ?? 'kept'}:${picture.id}`)}
      <div class={cn('rounded-md p-1', state && surface[state])}>
        {#if state}<DiffMarker {state} />{/if}
        <PictureImage {picture} readonly />
      </div>
    {/each}
  </div>
{:else}
  <div class="text-muted-foreground p-1">{$t`No pictures`}</div>
{/if}
