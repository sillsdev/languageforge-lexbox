<script lang="ts">
  import type {IMultiString, IRichMultiString, IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {asString} from '$project/data';
  import DiffMultiString from './DiffMultiString.svelte';

  // Diffs the plain text, so a formatting-only change reads as unchanged.
  let {before, after, writingSystems}: {
    before?: IRichMultiString;
    after?: IRichMultiString;
    writingSystems: ReadonlyArray<ReadonlyDeep<IWritingSystem>>;
  } = $props();

  function toPlain(rich?: IRichMultiString): IMultiString | undefined {
    if (!rich) return undefined;
    return Object.fromEntries(Object.entries(rich).map(([wsId, value]) => [wsId, asString(value) ?? '']));
  }
</script>

<DiffMultiString before={toPlain(before)} after={toPlain(after)} {writingSystems} />
