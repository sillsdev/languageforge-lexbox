<script lang="ts">
  import type {IMultiString, IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {Label} from '$lib/components/ui/label';
  import DiffText from './DiffText.svelte';
  import DiffShell from './DiffShell.svelte';

  let {before, after, writingSystems}: {
    before?: IMultiString;
    after?: IMultiString;
    writingSystems: ReadonlyArray<ReadonlyDeep<IWritingSystem>>;
  } = $props();

  const visibleWritingSystems = $derived(writingSystems.filter((ws) => !ws.isAudio));
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each visibleWritingSystems as ws (ws.wsId)}
    <div class="grid gap-y-1 @lg/editor:grid-cols-subgrid col-span-full items-baseline" title={`${ws.name} (${ws.wsId})`}>
      <Label>{ws.abbreviation}</Label>
      <DiffShell>
        <DiffText before={before?.[ws.wsId]} after={after?.[ws.wsId]} />
      </DiffShell>
    </div>
  {/each}
</div>
