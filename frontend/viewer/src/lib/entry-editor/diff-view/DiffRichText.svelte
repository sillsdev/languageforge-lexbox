<script lang="ts">
  import type {IRichMultiString, IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {Label} from '$lib/components/ui/label';
  import {asString} from '$project/data';
  import DiffText from './DiffText.svelte';
  import DiffShell from './DiffShell.svelte';

  let {before, after, writingSystems}: {
    before?: IRichMultiString;
    after?: IRichMultiString;
    writingSystems: ReadonlyArray<ReadonlyDeep<IWritingSystem>>;
  } = $props();

  const visibleWritingSystems = $derived(writingSystems.filter((ws) => !ws.isAudio));
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each visibleWritingSystems as ws (ws.wsId)}
    <div class="grid gap-y-1 @lg/editor:grid-cols-subgrid col-span-full items-baseline" title={`${ws.name} (${ws.wsId})`}>
      <Label>{ws.abbreviation}</Label>
      <DiffShell>
        <DiffText before={asString(before?.[ws.wsId])} after={asString(after?.[ws.wsId])} />
      </DiffShell>
    </div>
  {/each}
</div>
