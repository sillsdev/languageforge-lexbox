<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {t} from 'svelte-i18n-lingui';
  import EntryEditor from '../entry-editor/object-editors/EntryEditor.svelte';
  import ExampleEditorPrimitive from '../entry-editor/object-editors/ExampleEditorPrimitive.svelte';
  import SenseEditorPrimitive from '../entry-editor/object-editors/SenseEditorPrimitive.svelte';
  import {type HistoryItem, useHistoryService} from '../services/history-service';
  import {EditorGrid} from '$lib/components/editor';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import ListItem from '$lib/components/ListItem.svelte';
  import {VList} from 'virtua/svelte';
  import {FormatDuration, formatDuration} from '$lib/components/ui/format';

  export let id: string;
  export let open: boolean;

  useBackHandler({
    addToStack: () => open,
    onBack: () => open = false,
    key: 'history-view'
  });

  let loading = false;
  let record: HistoryItem | undefined;
  const historyService = useHistoryService();
  let history: HistoryItem[];

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
      <div class="grid gap-x-6 gap-y-1" style="grid-template-rows: auto minmax(0,100%); grid-template-columns: minmax(min-content, 1fr) minmax(min-content, 2fr);">
        <div class="flex flex-col gap-4 row-start-2">
          <div class="h-full rounded-md">
            {#if !history || history.length === 0}
              <div class="p-4 text-center opacity-75">{$t`No history found`}</div>
            {:else}
              <VList data={history}
                     getKey={row => `${row.commitId}_${row.changeIndex}`}
                     class="h-full p-0.5 md:pr-3 after:h-12 after:block">
                {#snippet children(row)}
                  <ListItem
                    onclick={() => showEntry(row)}
                    class="mb-2"
                    selected={record?.commitId === row.commitId}>
                    <span>{row.changeName ?? $t`No change name`}</span>
                    <div class="text-sm text-muted-foreground">
                      {#if row.previousTimestamp}
                        <FormatDuration
                          start={new Date(row.timestamp)}
                          end={new Date(row.previousTimestamp)}
                          smallestUnit="seconds"
                          options={{style: 'narrow'}}/>
                        {$t`before`}
                      {:else}
                        <FormatDuration
                          start={new Date(row.timestamp)}
                          smallestUnit="seconds"
                          options={{style: 'narrow'}}/>
                        {$t`ago`}
                      {/if}
                    </div>
                  </ListItem>
                  {/snippet}
              </VList>
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
<!--              <div class="hidden sm:contents">-->
<!--                <ShowEmptyFieldsSwitch bind:value={showEmptyFields}/>-->
<!--              </div>-->
            </div>
            <div class="col-start-2 row-start-2 overflow-auto p-3 pt-2 border rounded h-max max-h-full">
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
