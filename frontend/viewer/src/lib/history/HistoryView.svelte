<script lang="ts">
  import ShowEmptyFieldsSwitch from '$lib/layout/ShowEmptyFieldsSwitch.svelte';
  import * as Dialog from '$lib/components/ui/dialog';
  import {cls, Duration, DurationUnits, InfiniteScroll, ListItem} from 'svelte-ux';
  import {t} from 'svelte-i18n-lingui';
  import EntryEditor from '../entry-editor/object-editors/EntryEditor.svelte';
  import ExampleEditorPrimitive from '../entry-editor/object-editors/ExampleEditorPrimitive.svelte';
  import SenseEditorPrimitive from '../entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import {type HistoryItem, useHistoryService} from '../services/history-service';
  import {EditorGrid} from '$lib/components/editor';

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
  $: if (!open) reset();

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

<Dialog.Root bind:open>
  <Dialog.DialogContent interactOutsideBehavior={loading ? 'ignore' : 'close'} class="flex flex-col">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`History`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    {#if !loading}
      <div class="grid gap-x-6 gap-y-1 overflow-hidden" style="grid-template-rows: auto minmax(0,100%); grid-template-columns: minmax(min-content, 1fr) minmax(min-content, 2fr);">
        <div class="flex flex-col gap-4 overflow-hidden row-start-2">
          <div class="border rounded-md overflow-y-auto">
            {#if !history || history.length === 0}
              <div class="p-4 text-center opacity-75">{$t`No history found`}</div>
            {:else}
              <InfiniteScroll perPage={50} items={history} let:visibleItems>
                {#each visibleItems as row (`${row.commitId}_${row.changeIndex}`)}
                  <ListItem
                    title={row.changeName ?? $t`No change name`}
                    on:click={() => showEntry(row)}
                    noShadow
                    class={cls(record?.commitId === row.commitId ? 'bg-primary/20 dark:bg-primary/20 selected-entry' : 'dark:bg-muted/50 bg-muted/80 hover:bg-muted/30 hover:dark:bg-muted')}>
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
          {#if record?.entity && record?.entityName}
            <div class="col-start-2 row-start-1 text-sm flex justify-between items-center px-2 pb-0.5">
          <span>{$t`Author:`}
            {#if record.authorName}
              <span class="font-semibold">{record.authorName}</span>
            {:else}
              <span class="opacity-75 italic">{$t`Unknown`}</span>
            {/if}
          </span>
              <div class="hidden sm:contents">
                <ShowEmptyFieldsSwitch bind:value={showEmptyFields}/>
              </div>
            </div>
            <div class="col-start-2 row-start-2 overflow-auto p-3 pt-2 border rounded h-max max-h-full"
                 class:hide-unused={!showEmptyFields}>
              {#key record}
                {#if record.entityName === 'Entry'}
                  <EntryEditor entry={record.entity} modalMode readonly/>
                {:else if record.entityName === 'Sense'}
                  <EditorGrid>
                        <SenseEditorPrimitive sense={record.entity} readonly/>
                  </EditorGrid>
                {:else if record.entityName === 'ExampleSentence'}
                  <EditorGrid>
                        <ExampleEditorPrimitive example={record.entity} readonly/>
                  </EditorGrid>
                {/if}
              {/key}
            </div>
          {/if}
        </div>
      </div>
    {/if}
  </Dialog.DialogContent>
</Dialog.Root>
