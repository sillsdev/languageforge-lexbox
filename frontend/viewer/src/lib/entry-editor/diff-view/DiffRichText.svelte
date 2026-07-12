<script lang="ts">
  import type {IMultiString, IRichMultiString, IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {asString} from '$project/data';
  import DiffMultiString from './DiffMultiString.svelte';

  // Rich strings diff as their plain text (span formatting isn't shown in previews), so this just
  // flattens each alternative and delegates the layout to DiffMultiString.
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
