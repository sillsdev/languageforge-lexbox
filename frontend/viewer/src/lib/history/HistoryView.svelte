<script lang="ts">
  import {mdiClose, mdiHistory} from '@mdi/js';
  import {Button, cls, Dialog, Duration, DurationUnits, InfiniteScroll, ListItem, Toggle} from 'svelte-ux';
  import EntryEditor from '../entry-editor/object-editors/EntryEditor.svelte';
  import SenseEditor from '../entry-editor/object-editors/SenseEditor.svelte';
  import ExampleEditor from '../entry-editor/object-editors/ExampleEditor.svelte';
  import {type HistoryItem, useHistoryService} from '../services/history-service';
  import ShowEmptyFieldsSwitch from '$lib/layout/ShowEmptyFieldsSwitch.svelte';

  export let id: string;
  export let small: boolean = false;
  let loading = false;
  let record: HistoryItem | undefined;
  const historyService = useHistoryService();
  let history: HistoryItem[];

  let showEmptyFields = false;

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
    <div class="sm-form:hidden" class:hidden={small}>
      History
    </div>
  </Button>
  <Dialog {open} on:close={toggleOff} {loading} persistent={loading}>
    <Button on:click={toggleOff} icon={mdiClose} class="absolute right-2 top-2 z-40" rounded="full"></Button>
    <div slot="title">History</div>
    <div class="m-4 mt-0 grid gap-x-6 gap-y-1 overflow-hidden" style="grid-template-columns: 250px 1fr; grid-template-rows: auto minmax(0,100%)">
      <div class="flex flex-col gap-4 overflow-hidden row-start-2">
        <div class="border rounded-md overflow-y-auto">
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
      {#if record?.entity && record?.entityName}
        <div class="col-start-2 row-start-1 text-sm flex justify-between items-center px-2 pb-0.5">
          <span>Author:
            {#if record.authorName}
              <span class="font-semibold">{record.authorName}</span>
            {:else}
              <span class="opacity-75 italic">Unknown</span>
            {/if}
          </span>
          <ShowEmptyFieldsSwitch bind:value={showEmptyFields} />
        </div>
        <div class="col-start-2 row-start-2 overflow-auto p-3 pt-2 border rounded" class:hide-empty={!showEmptyFields}>
          {#if record.entityName === 'Entry'}
            <EntryEditor entry={record.entity} modalMode readonly/>
          {:else if record.entityName === 'Sense'}
            <div class="editor-grid">
              <SenseEditor sense={record.entity} readonly/>
            </div>
          {:else if record.entityName === 'ExampleSentence'}
            <div class="editor-grid">
              <ExampleEditor example={record.entity} readonly/>
            </div>
          {/if}
        </div>
      {/if}
    </div>
    <div class="flex-grow"></div>
  </Dialog>
</Toggle>
