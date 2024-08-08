<script lang="ts">
  import {mdiHistory} from '@mdi/js';
  import {Button, cls, Dialog, Duration, DurationUnits, InfiniteScroll, ListItem, Toggle} from 'svelte-ux';
  import type {IEntry, IExampleSentence, ISense} from '../mini-lcm';
  import EntryEditor from '../entry-editor/object-editors/EntryEditor.svelte';
  import {getContext} from 'svelte';
  import SenseEditor from '../entry-editor/object-editors/SenseEditor.svelte';
  import ExampleEditor from '../entry-editor/object-editors/ExampleEditor.svelte';

  type EntityType = { entity: IEntry, entityName: 'Entry' } | { entity: ISense, entityName: 'Sense' } | {
    entity: IExampleSentence,
    entityName: 'ExampleSentence'
  } | { entity: undefined, entityName: undefined };
  export let id: string;
  export let small: boolean = false;
  let loading = false;
  let record: typeof history[number] | undefined;
  let projectName = getContext<string>('project-name');
  let history: Array<{
    commitId: string,
    timestamp: string,
    previousTimestamp?: string,
    snapshotId: string,
    changeName: string | undefined,
  } & EntityType>;

  async function load() {
    loading = true;
    const data = await fetch(`/api/history/${projectName}/${id}`).then(res => res.json());
    if (!Array.isArray(data)) {
      console.error('Invalid history data', data);
      history = [];
      return;
    }
    for (let i = 0; i < data.length; i++) {
      let historyElement = data[i];
      historyElement.previousTimestamp = data[i + 1]?.timestamp;
    }
    // Reverse the history so that the most recent changes are at the top
    history = data.toReversed();
    record = history[0];
    loading = false;
  }

  async function showEntry(row: typeof history[number]) {
    if (!row.entity || !row.snapshotId) {
      const data = await fetch(`/api/history/${projectName}/snapshot/at/${row.timestamp}?entityId=${id}`).then(res => res.json());
      record = {...row, entity: data.entity, entityName: data.typeName};
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
    <div class="m-6 grid gap-x-6" style="grid-template-columns: 2fr 4fr">

      <div class="side-scroller flex flex-col gap-4">
        <div class="border rounded-md overflow-auto">
          {#if !history || history.length === 0}
            <div class="p-4 text-center opacity-75">No history found</div>
          {:else}
            <InfiniteScroll perPage={10} items={history} let:visibleItems>
              {#each visibleItems as row (row.timestamp)}
                <ListItem
                  title={row.changeName ?? 'No change name'}
                  on:click={() => showEntry(row)}
                  noShadow
                  class={cls(
              'cursor-pointer',
              'hover:bg-surface-300',
              record?.commitId === row.commitId ? 'bg-surface-200 selected-entry' : ''
            )}>
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
      {#if record?.entity}
        {#if record.entityName === "Entry"}
          <EntryEditor readonly entry={record.entity} modalMode/>
        {:else if record.entityName === "Sense"}
          <div class="editor-grid">
            <SenseEditor sense={record.entity}/>
          </div>
        {:else if record.entityName === "ExampleSentence"}
          <div class="editor-grid">
            <ExampleEditor example={record.entity}/>
          </div>
        {/if}
      {/if}
    </div>
    <div class="flex-grow"></div>
  </Dialog>
</Toggle>
