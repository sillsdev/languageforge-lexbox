<script lang="ts">
  import type {IMultiString, IWritingSystem} from '$lib/dotnet-types';
  import type {ReadonlyDeep} from 'type-fest';
  import WsCode from '$lib/components/writing-system/WsCode.svelte';
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
    <!-- Self-contained 2-col row so WS-code + value stay side-by-side regardless of the ancestor
         container width. The editor's `@lg/editor:grid-cols-subgrid` variant only aligns with the
         ambient editor grid when the container ≥ 1024px, which the activity preview pane often isn't. -->
    <div class="grid gap-x-2 gap-y-1 grid-cols-[max-content_1fr] col-span-full items-baseline" title={`${ws.name} (${ws.wsId})`}>
      <WsCode abbreviation={ws.abbreviation} />
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
