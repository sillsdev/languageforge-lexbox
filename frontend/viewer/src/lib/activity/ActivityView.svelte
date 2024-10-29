<script lang="ts">
  import {mdiHistory} from '@mdi/js';
  import {
    cls,
    DurationUnits,
    Toggle,
    Button,
    Dialog,
    InfiniteScroll,
    ListItem,
    Duration,
    TextField
  } from 'svelte-ux';

  let loading = false;
  export let projectName: string;
  let activity: Array<{
    commitId: string,
    changeName: string,
    timestamp: string,
    previousTimestamp?: string,
    changes: object[]
  }>;
  let selectedRow: typeof activity[number] | undefined;

  async function load() {
    activity = [];
    loading = true;
    const data = await fetch(`/api/activity/${projectName}`).then(res => res.json());
    data.reverse();
    loading = false;
    if (!Array.isArray(data)) {
      console.error('Invalid history data', data);
      activity = [];
      return;
    }
    for (let i = 0; i < data.length; i++) {
      let row = data[i];
      row.previousTimestamp = data[i + 1]?.timestamp;
    }
    // Reverse the history so that the most recent changes are at the top
    activity = data.toReversed();
    selectedRow = activity[0];
  }
</script>

<Toggle let:on={open} let:toggleOn let:toggleOff on:toggleOn={load}>
  <Button on:click={toggleOn} icon={mdiHistory} variant="fill-outline" color="info" size="sm">
    <div class="hidden sm:contents">
      Activity
    </div>
  </Button>
  <Dialog {open} on:close={toggleOff} {loading} persistent={loading} class="w-[700px]">
    <div slot="title">Activity</div>
    <div class="m-6 grid gap-x-6 h-[50vh]" style="grid-template-columns: auto 4fr">
      <div class="flex flex-col gap-4 overflow-y-auto">
        <div class="border rounded-md">
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
        <TextField label="Changes"
                   value={JSON.stringify(selectedRow.changes, null, 4)}
                   multiline
                   class="readonly"
                   classes={{input: 'h-80'}}/>
      {/if}
    </div>
    <div class="flex-grow"></div>
  </Dialog>
</Toggle>
