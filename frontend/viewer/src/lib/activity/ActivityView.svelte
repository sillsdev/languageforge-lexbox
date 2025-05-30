<script lang="ts">
  import type { ICommitMetadata } from '$lib/dotnet-types/generated-types/SIL/Harmony/Core/ICommitMetadata';
  import { useHistoryService } from '$lib/services/history-service';
  import { cls, Duration, DurationUnits, InfiniteScroll, ListItem } from 'svelte-ux';
  import { t } from 'svelte-i18n-lingui';
  import { useProjectContext } from '$lib/project-context.svelte';
  import { resource } from 'runed';

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
    if (!projectCode) return [];
    const data = (await historyService.activity(projectContext.projectCode)) as Activity[];
    console.debug('Activity data', data);
    if (!Array.isArray(data)) {
      console.error('Invalid history data', data);
      return [];
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
    initialValue: [],
  });

  let selectedRow = $derived(activity.current[0]);

  function formatJsonForUi(json: object) {
    return JSON.stringify(json, null, 2)
      .split('\n') // Split into lines
      .slice(1, -1) // Remove the first and last line
      .map(line => line.slice(2)) // Remove one level of indentation
      .join('\n'); // Join the lines back together;
  }
</script>

{#if !activity.loading}
  <div class="m-4 grid gap-x-6 gap-y-1 overflow-hidden"
       style="grid-template-rows: auto minmax(0,100%); minmax(min-content, 1fr) minmax(min-content, 2fr)">
    <div class="flex flex-col gap-4 overflow-hidden row-start-2">
      <div class="border rounded-md overflow-y-auto">
        {#if activity.current.length === 0}
          <div class="p-4 text-center opacity-75">{$t`No activity found`}</div>
        {:else}
          <InfiniteScroll perPage={50} items={activity.current} let:visibleItems>
            {#each visibleItems as row (row.timestamp)}
              <ListItem
                title={row.changeName}
                noShadow
                on:click={() => selectedRow = row}
                class={cls(selectedRow?.commitId === row.commitId ? 'bg-primary/20 dark:bg-primary/20 selected-entry' : 'dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted')}>
                <div slot="subheading" class="text-sm">
                  {#if row.previousTimestamp}
                    <Duration totalUnits={2} start={new Date(row.timestamp)}
                              end={new Date(row.previousTimestamp)}
                              minUnits={DurationUnits.Second}/>
                    {$t`before`}
                  {:else}
                    <Duration totalUnits={2} start={new Date(row.timestamp)} minUnits={DurationUnits.Second}/>
                    {$t`ago`}
                  {/if}
                </div>
              </ListItem>
            {/each}
          </InfiniteScroll>
        {/if}
      </div>
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
              <Duration totalUnits={2} minUnits={DurationUnits.Second}
                        start={new Date(selectedRow.metadata.extraMetadata['SyncDate'])}/> {$t`ago`}
            </span>
          {/if}
        </div>
        <div
          class="change-list col-start-2 row-start-2 flex flex-col gap-4 overflow-auto p-1 border rounded h-max max-h-full">
          <InfiniteScroll perPage={100} items={selectedRow.changes} let:visibleItems>
            {#each visibleItems as change}
              <div class="change whitespace-pre-wrap font-mono text-sm">
                {formatJsonForUi(change)}
              </div>
            {/each}
          </InfiniteScroll>
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
