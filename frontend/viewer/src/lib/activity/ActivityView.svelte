<script lang="ts">
  import type {ICommitMetadata} from '$lib/dotnet-types/generated-types/SIL/Harmony/Core/ICommitMetadata';
  import {useHistoryService} from '$lib/services/history-service';
  import {mdiClose} from '@mdi/js';
  import {
    Button,
    cls,
    Dialog,
    Duration,
    DurationUnits,
    InfiniteScroll,
    ListItem
  } from 'svelte-ux';

  const historyService = useHistoryService();

  let loading = false;

  export let open: boolean;
  export let projectName: string;

  type Activity = {
    commitId: string,
    changeName: string,
    timestamp: string,
    previousTimestamp?: string,
    changes: object[],
    metadata: ICommitMetadata
  };
  let activity: Array<Activity>;
  let selectedRow: Activity | undefined;

  // loaded makes hot-reload work
  let loaded = false;
  $: loaded = !open;
  if (!loaded) void load();

  async function load() {
    loaded = true;
    activity = [];
    loading = true;
    const data = await historyService.activity(projectName) as Activity[];
    console.debug('Activity data', data);
    loading = false;
    if (!Array.isArray(data)) {
      console.error('Invalid history data', data);
      activity = [];
      return;
    }
    data.reverse();
    for (let i = 0; i < data.length; i++) {
      let row = data[i];
      row.previousTimestamp = data[i + 1]?.timestamp;
    }
    // Reverse the history so that the most recent changes are at the top
    activity = data.toReversed();
    selectedRow = activity[0];
  }

  function formatJsonForUi(json: object) {
    return JSON.stringify(json, null, 2)
      .split('\n') // Split into lines
      .slice(1, -1) // Remove the first and last line
      .map(line => line.slice(2)) // Remove one level of indentation
      .join('\n'); // Join the lines back together;
  }
</script>

<Dialog bind:open {loading} persistent={loading}>
  <Button on:click={() => open = false} icon={mdiClose} class="absolute right-2 top-2 z-40" rounded="full"></Button>
  <div slot="title">Activity</div>
  <div class="m-4 mt-0 grid gap-x-6 gap-y-1 overflow-hidden" style="grid-template-rows: auto minmax(0,100%)">
    <div class="flex flex-col gap-4 overflow-hidden row-start-2">
      <div class="border rounded-md overflow-y-auto">
        {#if !activity || activity.length === 0}
          <div class="p-4 text-center opacity-75">No activity found</div>
        {:else}
          <InfiniteScroll perPage={10} items={activity} let:visibleItems>
            {#each visibleItems as row (row.timestamp)}
              <ListItem
                title={row.changeName}
                noShadow
                on:click={() => selectedRow = row}
                class={cls(selectedRow?.commitId === row.commitId ? 'bg-surface-200 selected-entry' : '')}>
                <div slot="subheading" class="text-sm text-surface-content/50">
                  {#if row.previousTimestamp}
                    <Duration totalUnits={2} start={new Date(row.timestamp)}
                              end={new Date(row.previousTimestamp)}
                              minUnits={DurationUnits.Second}/>
                    before
                  {:else}
                    <Duration totalUnits={2} start={new Date(row.timestamp)} minUnits={DurationUnits.Second}/>
                    ago
                  {/if}
                </div>
              </ListItem>
            {/each}
          </InfiniteScroll>
        {/if}
      </div>
    </div>

    {#if selectedRow}
      <div class="col-start-2 row-start-1 text-sm">
        <span>Author:
          {#if selectedRow.metadata.authorName}
            <span class="font-semibold">{selectedRow.metadata.authorName}</span>
          {:else}
            <span class="opacity-75 italic">Unknown</span>
          {/if}
        </span>
        {#if selectedRow.changes.length > 1}
          <span>– ({selectedRow.changes.length} changes)</span>
        {/if}
      </div>
      <div class="col-start-2 row-start-2 flex flex-col gap-4 overflow-auto p-1 border rounded h-max max-h-full change-list">
        <InfiniteScroll perPage={100} items={selectedRow.changes} let:visibleItems>
          {#each visibleItems as change}
            <div class="whitespace-pre-wrap font-mono text-sm [&:not(> :last-child)]:border-b [&:not(> :last-child)]:pb-4">
              {formatJsonForUi(change)}
            </div>
          {/each}
        </InfiniteScroll>
      </div>
    {/if}
  </div>
  <div class="flex-grow"></div>
</Dialog>

<style lang="postcss" global>
  .change-list .sentinel {
    @apply -mt-4; /* make gap-4 not apply to the infinite-scroll end detector */
  }
</style>
