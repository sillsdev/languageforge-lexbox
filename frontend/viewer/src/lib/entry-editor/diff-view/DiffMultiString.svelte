<script lang="ts">
  import type {IMultiString, IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import {Label} from '$lib/components/ui/label';
  import DiffText from './DiffText.svelte';
  import DiffShell from './DiffShell.svelte';
  import DiffAudio from './DiffAudio.svelte';

  let {before, after, writingSystems}: {
    before?: IMultiString;
    after?: IMultiString;
    writingSystems: ReadonlyArray<ReadonlyDeep<IWritingSystem>>;
  } = $props();
</script>

<div class="grid grid-cols-subgrid col-span-full gap-y-2">
  {#each writingSystems as ws (ws.wsId)}
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
