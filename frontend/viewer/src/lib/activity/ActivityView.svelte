<script lang="ts">
  import type { ICommitMetadata } from '$lib/dotnet-types/generated-types/SIL/Harmony/Core/ICommitMetadata';
  import { useHistoryService } from '$lib/services/history-service';
  import { t } from 'svelte-i18n-lingui';
  import { useProjectContext } from '$lib/project-context.svelte';
  import { resource } from 'runed';
  import {SidebarTrigger} from '$lib/components/ui/sidebar';
  import ListItem from '$lib/components/ListItem.svelte';
  import {VList} from 'virtua/svelte';
  import {FormatDuration, formatDuration} from '$lib/components/ui/format';

  const historyService = useHistoryService();
  const projectContext = useProjectContext();

  type Activity = {
    commitId: string;
    changeName: string;
    timestamp: string;
    previousTimestamp?: string;
    changes: object[];
    metadata: ICommitMetadata;
  };

  const activity = resource(() => projectContext.projectCode, async (projectCode) => {
    if (!projectCode) return [] as Activity[];
    const data = (await historyService.activity(projectContext.projectCode)) as Activity[];
    console.debug('Activity data', data);
    if (!Array.isArray(data)) {
      console.error('Invalid history data', data);
      return [] as Activity[];
    }
    data.reverse();
    for (let i = 0; i < data.length; i++) {
      let row = data[i];
      row.previousTimestamp = data[i + 1]?.timestamp;
    }
    // Reverse the history so that the most recent changes are at the top
    return data.toReversed();
  },
  {
    initialValue: [] as Activity[],
  });

  let selectedRow = $derived(activity.current[0]);

  function formatJsonForUi(json: object) {
    return JSON.stringify(json, null, 2)
      .split('\n') // Split into lines
      .slice(1, -1) // Remove the first and last line
      .map(line => line.slice(2)) // Remove one level of indentation
      .join('\n'); // Join the lines back together;
  }
  const zeroS = formatDuration({seconds: 1}, 'seconds', {style: 'narrow'}).replace('1', '0');
</script>

{#if !activity.loading}
  <div class="h-full m-4 grid gap-x-6 gap-y-1 overflow-hidden"
       style="grid-template-rows: auto minmax(0,100%); minmax(min-content, 1fr) minmax(min-content, 2fr); grid-template-columns: 1fr 2fr">

    <SidebarTrigger icon="i-mdi-menu" iconProps={{ class: 'size-5' }} class="aspect-square p-0" size="xs"/>
    <div class="gap-4 overflow-hidden row-start-2">

      <VList data={activity.current ?? []}
             class="h-full p-0.5 md:pr-3 after:h-12 after:block"
             getKey={row => row.timestamp} overscan={10}>
        {#snippet children(row)}
          <ListItem
            onclick={() => selectedRow = row}
            selected={selectedRow?.commitId === row.commitId}
            class="mb-2">
            <span>{row.changeName}</span>
            <div class="text-sm text-muted-foreground">
              {#if row.previousTimestamp}
                <FormatDuration start={new Date(row.timestamp)}
                                end={new Date(row.previousTimestamp)}
                                smallestUnit="seconds"
                                options={{style: 'narrow'}}/>
                {$t`before`}
              {:else}
                <FormatDuration start={new Date(row.timestamp)}
                                smallestUnit="seconds"
                                options={{style: 'narrow'}}/>
                {$t`ago`}
              {/if}
            </div>
          </ListItem>
        {/snippet}
      </VList>
      {#if activity.current.length === 0}
        <div class="p-4 text-center opacity-75">{$t`No activity found`}</div>
      {/if}
    </div>

    <div class="grid grid-cols-subgrid grid-rows-subgrid col-start-2 row-span-2">
      {#if selectedRow}
        <div class="col-start-2 row-start-1 text-sm">
          <span>{$t`Author:`}
            {#if selectedRow.metadata.authorName}
              <span class="font-semibold">{selectedRow.metadata.authorName}</span>
            {:else}
              <span class="opacity-75 italic">{$t`Unknown`}</span>
            {/if}
          </span>
          {#if selectedRow.changes.length > 1}
            <span>{$t`– (${selectedRow.changes.length} changes)`}</span>
          {/if}
          {#if selectedRow.metadata.extraMetadata['SyncDate']}
            <span class="float-right">
              {$t`Synced`}
              {formatDuration({seconds: (new Date().getTime() - new Date(selectedRow.metadata.extraMetadata['SyncDate']).getTime()) / 1000}, 'seconds', {style: 'narrow'})}
              {$t`ago`}
            </span>
          {/if}
        </div>
        <div
          class="change-list col-start-2 row-start-2 flex flex-col gap-4 overflow-auto p-1 border rounded">
          <VList data={selectedRow.changes}>
            {#snippet children(change)}
              <div class="change whitespace-pre-wrap font-mono text-sm">
                {formatJsonForUi(change)}
              </div>
            {/snippet}
          </VList>
        </div>
      {/if}
    </div>
  </div>
{/if}

<style lang="postcss">
  :global(.change-list .sentinel) {
    @apply -mt-4; /* make gap-4 not apply to the infinite-scroll end detector */
  }
  .change-list .change:not(:nth-last-child(2)) { /* 2, because InfiniteScroll adds an additional element */
    @apply border-b pb-4;
  }
</style>
