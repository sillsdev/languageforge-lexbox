﻿<script lang="ts">
  import ShowEmptyFieldsSwitch from '$lib/layout/ShowEmptyFieldsSwitch.svelte';
  import {mdiClose} from '@mdi/js';
  import {Button, cls, Dialog, Duration, DurationUnits, InfiniteScroll, ListItem} from 'svelte-ux';
  import EntryEditor from '../entry-editor/object-editors/EntryEditor.svelte';
  import ExampleEditor from '../entry-editor/object-editors/ExampleEditor.svelte';
  import SenseEditor from '../entry-editor/object-editors/SenseEditor.svelte';
  import {type HistoryItem, useHistoryService} from '../services/history-service';

  export let id: string;
  export let open: boolean;

  let loading = false;
  let record: HistoryItem | undefined;
  const historyService = useHistoryService();
  let history: HistoryItem[];

  let showEmptyFields = false;

  $: if (open && id) {
    void load();
  }

  async function load() {
    loading = true;
    try {
      history = [];
      history = await historyService.load(id);
      record = history[0];
    } finally {
      loading = false;
    }
  }

  async function showEntry(row: HistoryItem) {
    if (!row.entity || !row.snapshotId) {
      record = await historyService.fetchSnapshot(row, id);
    } else {
      record = row;
    }
  }

  function reset() {
    record = undefined;
    history = [];
  }
</script>

<Dialog bind:open {loading} persistent={loading} on:close={reset}>
  <Button on:click={() => open = false} icon={mdiClose} class="absolute right-2 top-2 z-40" rounded="full"></Button>
  <div slot="title">History</div>
  {#if !loading}
  <div class="m-4 mt-0 grid gap-x-6 gap-y-1 overflow-hidden" style="grid-template-rows: auto minmax(0,100%); grid-template-columns: minmax(min-content, 1fr) minmax(min-content, 2fr);">
    <div class="flex flex-col gap-4 overflow-hidden row-start-2">
      <div class="border rounded-md overflow-y-auto">
        {#if !history || history.length === 0}
          <div class="p-4 text-center opacity-75">No history found</div>
        {:else}
          <InfiniteScroll perPage={50} items={history} let:visibleItems>
            {#each visibleItems as row (`${row.commitId}_${row.changeIndex}`)}
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
    <div class="grid grid-cols-subgrid grid-rows-subgrid col-start-2 row-span-2">
      {#if record?.entity && record?.entityName}
        <div class="col-start-2 row-start-1 text-sm flex justify-between items-center px-2 pb-0.5">
          <span>Author:
            {#if record.authorName}
              <span class="font-semibold">{record.authorName}</span>
            {:else}
              <span class="opacity-75 italic">Unknown</span>
            {/if}
          </span>
          <div class="hidden sm:contents">
            <ShowEmptyFieldsSwitch bind:value={showEmptyFields} />
          </div>
        </div>
        <div class="col-start-2 row-start-2 overflow-auto p-3 pt-2 border rounded h-max max-h-full" class:hide-unused={!showEmptyFields}>
          {#key record}
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
          {/key}
        </div>
      {/if}
    </div>
  </div>
  {/if}
  <div class="flex-grow"></div>
</Dialog>
