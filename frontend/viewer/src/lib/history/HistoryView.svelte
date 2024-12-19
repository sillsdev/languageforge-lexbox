<script lang="ts">
  import {mdiHistory} from '@mdi/js';
  import {Button, cls, Dialog, Duration, DurationUnits, InfiniteScroll, ListItem, Toggle} from 'svelte-ux';
  import EntryEditor from '../entry-editor/object-editors/EntryEditor.svelte';
  import SenseEditor from '../entry-editor/object-editors/SenseEditor.svelte';
  import ExampleEditor from '../entry-editor/object-editors/ExampleEditor.svelte';
  import {type HistoryItem, useHistoryService} from '../services/history-service';

  export let id: string;
  export let small: boolean = false;
  let loading = false;
  let record: HistoryItem | undefined;
  const historyService = useHistoryService();
  let history: HistoryItem[];

  async function load() {
    loading = true;
    history = await historyService.load(id);
    record = history[0];
    loading = false;
  }

  async function showEntry(row: HistoryItem) {
    if (!row.entity || !row.snapshotId) {
      record = await historyService.fetchSnapshot(row, id);
    } else {
      record = row;
    }
  }
</script>
<Toggle let:on={open} let:toggleOn let:toggleOff on:toggleOn={load}>
  <Button on:click={toggleOn} icon={mdiHistory} variant="fill-light" color="info" size="sm">
    <div class="hidden" class:sm:contents={!small}>
      View History
    </div>
  </Button>
  <Dialog {open} on:close={toggleOff} {loading} persistent={loading} class="w-[700px]">
    <div slot="title">History</div>
    <div class="m-6 grid gap-x-6 h-[50vh]" style="grid-template-columns: auto 4fr;">

      <div class="flex flex-col gap-4 overflow-y-auto">
        <div class="border rounded-md">
          {#if !history || history.length === 0}
            <div class="p-4 text-center opacity-75">No history found</div>
          {:else}
            <InfiniteScroll perPage={10} items={history} let:visibleItems>
              {#each visibleItems as row (row.timestamp)}
                <ListItem
                  title={row.changeName ?? 'No change name'}
                  on:click={() => showEntry(row)}
                  noShadow
                  class={cls(record?.commitId === row.commitId ? 'bg-surface-200 selected-entry' : '')}>
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
      <div>
        {#if record?.entity}
          {#if record.entityName === 'Entry'}
            <EntryEditor entry={record.entity} modalMode/>
          {:else if record.entityName === 'Sense'}
            <div class="editor-grid">
              <SenseEditor sense={record.entity}/>
            </div>
          {:else if record.entityName === 'ExampleSentence'}
            <div class="editor-grid">
              <ExampleEditor example={record.entity} readonly={false}/>
            </div>
          {/if}
        {/if}
      </div>
    </div>
    <div class="flex-grow"></div>
  </Dialog>
</Toggle>
