<script lang="ts">
  import type {IMultiString, IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {Label} from '$lib/components/ui/label';
  import DiffText from './DiffText.svelte';
  import DiffShell from './DiffShell.svelte';
  import DiffAudio from './DiffAudio.svelte';
  import {useWritingSystemService} from '$project/data';

  let {before, after, writingSystems}: {
    before?: IMultiString;
    after?: IMultiString;
    writingSystems: ReadonlyArray<ReadonlyDeep<IWritingSystem>>;
  } = $props();

  const writingSystemService = useWritingSystemService();

  // Writing systems the view hides still show when this change touched them, or the diff would look empty.
  const shown = $derived.by(() => {
    const ids = new Set(writingSystems.map((ws) => ws.wsId));
    const hidden = [...new Set([...Object.keys(before ?? {}), ...Object.keys(after ?? {})])]
      .filter((wsId) => !ids.has(wsId) && (before?.[wsId] ?? '') !== (after?.[wsId] ?? ''))
      .map((wsId) => writingSystemService.getWritingSystem(wsId) ?? {wsId, name: wsId, abbreviation: wsId, isAudio: false});
    return [...writingSystems, ...hidden];
  });
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each shown as ws (ws.wsId)}
    <!-- Label always beside its value: the editor's rows only do that once the editor container reaches
         @lg (32rem), and diffs often render in a narrower pane. -->
    <div class="grid gap-x-2 gap-y-1 grid-cols-[max-content_1fr] col-span-full items-baseline" title={`${ws.name} (${ws.wsId})`}>
      <Label>{ws.abbreviation}</Label>
      {#if ws.isAudio}
        <DiffAudio before={before?.[ws.wsId]} after={after?.[ws.wsId]} />
      {:else}
        <DiffShell>
          <DiffText before={before?.[ws.wsId]} after={after?.[ws.wsId]} />
        </DiffShell>
      {/if}
    </div>
  {/each}
</div>
