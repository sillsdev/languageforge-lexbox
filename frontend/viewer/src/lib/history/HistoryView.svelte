<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {t} from 'svelte-i18n-lingui';
  import {type HistoryItem, useHistoryService} from '../services/history-service';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import ListItem from '$lib/components/ListItem.svelte';
  import {VList} from 'virtua/svelte';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import ActivityItem from '$lib/activity/ActivityItem.svelte';

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
    if (!row.entity) {
      const snapshot = await historyService.fetchSnapshot(row, id);
      Object.assign(row, snapshot);
    }
    record = row;
  }

  function reset() {
    record = undefined;
    history = [];
  }
</script>

<Dialog.Root bind:open>
  <Dialog.DialogContent interactOutsideBehavior={loading ? 'ignore' : 'close'} class="flex flex-col sm:min-h-[min(calc(100%-16px),30rem)] overflow-hidden w-[70rem]">
    <Dialog.DialogHeader>
      <Dialog.DialogTitle>{$t`History`}</Dialog.DialogTitle>
    </Dialog.DialogHeader>
    {#if !loading}
      <div class="grid gap-x-6 gap-y-1 grow overflow-hidden" style="grid-template-columns: minmax(min-content, 1fr) minmax(min-content, 2fr);">
        <div class="h-full overflow-hidden rounded-md">
          {#if !history || history.length === 0}
            <div class="p-4 text-center opacity-75">{$t`No history found`}</div>
          {:else}
            <VList data={history}
                    getKey={row => `${row.commitId}_${row.changeIndex}`}
                    class="h-full p-0.5 md:pr-3 after:h-12 after:block !contain-content">
              {#snippet children(row)}
                <ListItem
                  onclick={() => showEntry(row)}
                  class="mb-2"
                  selected={record?.commitId === row.commitId}>
                  <span>{row.changeName}</span>
                  <div class="text-sm text-muted-foreground flex flex-wrap gap-x-2 justify-between">
                    <span>
                      <FormatRelativeDate date={row.timestamp}
                              actualDateOptions={{ dateStyle: 'medium', timeStyle: 'short' }}/>
                    </span>
                    <span>
                      {row.metadata.authorName}
                    </span>
                  </div>
                </ListItem>
                {/snippet}
            </VList>
          {/if}
        </div>
        <div class="w-full">
          {#if record}
            <ActivityItem activity={{...record, changes: [record.change]}} />
          {/if}
        </div>
      </div>
    {/if}
  </Dialog.DialogContent>
</Dialog.Root>
