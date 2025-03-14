<script lang="ts" context="module">
  import type {IEntry, ISense} from '$lib/dotnet-types';

  export type EntrySenseSelection = {
    entry: IEntry;
    sense?: ISense;
  };
</script>

<script lang="ts">
  import {mdiBookPlusOutline, mdiBookSearchOutline, mdiMagnifyRemoveOutline, mdiPlus} from '@mdi/js';
  import { Button, Dialog, ExpansionPanel, Icon, ListItem, ProgressCircle, TextField } from 'svelte-ux';
  import { derived, writable } from 'svelte/store';
  import { createEventDispatcher, getContext } from 'svelte';
  import { useLexboxApi } from '../services/service-provider';
  import { deriveAsync } from '../utils/time';
  import { defaultSense } from '../utils';
  import { useProjectCommands } from '../commands';
  import type { SaveHandler } from '../services/save-event-service';
  import {SortField} from '$lib/dotnet-types';
  import {useWritingSystemService} from '$lib/writing-system-service';
  import NewEntryButton from './NewEntryButton.svelte';

  const dispatch = createEventDispatcher<{
    pick: EntrySenseSelection;
  }>();

  const projectCommands = useProjectCommands();
  const saveHandler = getContext<SaveHandler>('saveHandler');
  const writingSystemService = useWritingSystemService();

  export let open = false;
  export let title: string;
  export let disableEntry: ((entry: IEntry) => false | { reason: string, disableSenses?: true }) | undefined = undefined;
  export let disableSense: ((sense: ISense, entry: IEntry) => false | string) | undefined = undefined;
  export let mode: 'entries-and-senses' | 'only-entries' = 'entries-and-senses';
  $: onlyEntries = mode === 'only-entries';

  let selectedEntry: IEntry | undefined;
  let selectedSense: ISense | undefined;
  // We need this redundant field so the ExpandPanel has something to bind to. Otherwise it's very fragile.
  // So it's basically just for managing the state of the ExpansionPanel.
  let selectedEntryId: string | undefined;

  const lexboxApi = useLexboxApi();
  const search = writable<string>('');
  const fetchCount = 150;
  const displayCount = 50;

  let addedEntries: IEntry[] = [];
  const { value: result, loading } = deriveAsync(search, async (s) => {
    if (!s) return Promise.resolve({ entries: [], search: undefined });
    let entries = await lexboxApi.searchEntries(s ?? '', {
      offset: 0,
      count: fetchCount,
      order: {field: SortField.Headword, writingSystem: 'default', ascending: true},
    });
    return { entries, search: s};
  }, {entries: [], search: undefined}, 200);
  const displayedEntries = derived(result, (result) => {
    return result?.entries.slice(0, displayCount) ?? [];
  });

  $: {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    $result;
    addedEntries = [];
  }

  function onPick() {
    dispatch('pick', {entry: selectedEntry!, sense: selectedSense});
  }

  function reset() {
    $search = '';
    selectedEntry = undefined;
    selectedEntryId = undefined;
    selectedSense = undefined;
    addedEntries = [];
  }

  async function onClickAddSense(entry: IEntry): Promise<void> {
    const newSense = defaultSense(entry.id);
    const savedSense = await saveHandler(() => lexboxApi.createSense(entry.id, newSense));
    entry.senses = [...entry.senses, savedSense];
    selectedSense = savedSense;
    onPick();
    open = false;
  }


  async function onClickCreateNewEntry(): Promise<void> {
    const entry = await projectCommands.createNewEntry($search, { dontSelect: true });
    selectedEntry = entry;
    selectedEntryId = entry?.id;
    if (entry) {
      addedEntries = [entry];
    }
  }

  function select(entry?: IEntry, sense?: ISense): void {
    selectedEntry = entry;
    selectedEntryId = entry?.id;
    selectedSense = sense;
  }

  function onExpansionChange(open: boolean, entry: IEntry, disabledEntry: boolean) {
    if (open) { // I'm opening so I manage the state
      select(entry);
      return;
    }
      // a different entry was selected, I don't manage the state
    if (selectedEntry?.id !== entry.id) {
      return;
    }
    if (selectedSense && !disabledEntry) {
      // move selection to the entry and keep myself open
      select(entry);
    } else {
      // let myself close
      select();
    }
  }
</script>

<Dialog bind:open on:close={reset} class="entry-sense-picker" classes={{title: 'p-2'}}>
  <div slot="title">
    <h2 class="mb-4 mt-3 mx-2">
      {title}
    </h2>
    <TextField
      autofocus
      clearable
      bind:value={$search}
      placeholder="Find entry..."
      class="flex-grow-[2] cursor-pointer opacity-80 hover:opacity-100"
      classes={{ prepend: 'text-sm', append: 'flex-row-reverse'}}
      icon={mdiBookSearchOutline}>
      <div slot="append" class="flex p-1">
        {#if $loading}
          <ProgressCircle size={20} width={2} />
        {/if}
      </div>
    </TextField>
  </div>
  <div class="p-1">
    {#each [...$displayedEntries, ...addedEntries] as entry (entry.id)}
      {@const disabledEntry = disableEntry?.(entry)}
      {@const disableExpand = onlyEntries || (disabledEntry && disabledEntry.disableSenses)}
      <div class="entry"
        class:selected={entry.id === selectedEntryId && !selectedSense && !disabledEntry}
        class:disabled={disabledEntry}
        class:disable-expand={disableExpand}>
        <ExpansionPanel
          bind:group={selectedEntryId}
          value={entry.id}
          disabled={disableExpand}
          on:change={(event) => onExpansionChange(event.detail.open, entry, !!disabledEntry)}
        >
          <button disabled={!!disabledEntry} slot="trigger" class="flex-1 flex justify-between items-center text-left max-w-full overflow-hidden"
            on:click={() => {
              if (disableExpand) {
                // In this case, the ExpansionPanel' on:change event above is not in use, so we need to manage state here
                select(selectedEntry?.id === entry.id ? undefined : entry);
              }
            }}>
            <ListItem
              title={writingSystemService.headword(entry).padStart(1, '–')}
              subheading={writingSystemService.glosses(entry).padStart(1, '–')}
              noShadow />
            <div class="grow"></div>
            {#if disabledEntry}
              <span class="mr-2 shrink-0 h-7 px-2 justify-center inline-flex items-center border border-warning text-warning rounded-lg">
                {disabledEntry.reason}
              </span>
            {/if}
            {#if entry.senses.length && !onlyEntries}
              <span class="aspect-square w-7 mr-4 shrink-0 justify-center inline-flex items-center border border-info text-info rounded-lg">
                {entry.senses.length}
              </span>
            {/if}
          </button>
          {#each entry.senses as sense}
            {@const disabledSense = disableSense?.(sense, entry)}
            <span class="hidden"></span> <!-- so the first sense doesn't get :first styles, because the entry is the first list item -->
            <button class="sense w-full bg-surface-100 flex-1 flex justify-between items-center text-left max-w-full overflow-hidden"
              class:selected={selectedSense?.id === sense.id}
              class:disabled={disabledSense}
              on:click={() => selectedSense = selectedSense?.id === sense.id ? undefined : sense}>
              <ListItem
                title={writingSystemService.firstGloss(sense).padStart(1, '–')}
                subheading={writingSystemService.firstDef(sense).padStart(1, '–')}
                classes={{icon: 'text-info'}}
                noShadow />
              {#if disabledSense}
                <span class="mr-4 shrink-0 h-7 px-2 justify-center inline-flex items-center border border-warning text-warning rounded-lg">
                  {disabledSense}
                </span>
              {/if}
            </button>
          {/each}
          <ListItem
            title="Add Sense..."
            icon={mdiPlus}
            classes={{root: 'text-success py-4 border-none hover:bg-success-900/25'}}
            noShadow
            on:click={() => onClickAddSense(entry)}
          />
        </ExpansionPanel>
      </div>
    {/each}
    {#if $displayedEntries.length === 0 && addedEntries.length === 0}
      <div class="p-4 text-center opacity-75 flex justify-center items-center gap-2">
        {#if $result.search}
          No entries found <Icon data={mdiMagnifyRemoveOutline} />
          <NewEntryButton on:click={onClickCreateNewEntry} />
        {:else if $loading}
          <ProgressCircle size={30} />
        {:else}
            Search for an entry {onlyEntries ? '' : 'or sense'} <Icon data={mdiBookSearchOutline} /> or
            <NewEntryButton on:click={onClickCreateNewEntry} />
        {/if}
      </div>
    {/if}
    {#if $displayedEntries.length}
      <ListItem
        title="Create new Entry..."
        icon={mdiBookPlusOutline}
        classes={{root: 'text-success py-4 border-none rounded m-0.5 hover:bg-success-900/25'}}
        noShadow
        on:click={onClickCreateNewEntry}
      />
    {/if}
    {#if $result.entries.length > $displayedEntries.length}
      <div class="px-4 py-2 text-center opacity-75 flex items-center">
        <span>{$result.entries.length - $displayedEntries.length}</span>
        {#if $result.entries.length === fetchCount}<span>+</span>{/if}
        <div class="ml-1 flex justify-between items-center gap-2">
          <span>more matching entries...</span>
        </div>
      </div>
    {/if}
  </div>
  <div class="flex-grow"></div>
  <div slot="actions">
    <Button on:click={() => open = false}>Cancel</Button>
    <Button variant="fill-light" color="success" disabled={!selectedEntry || (!!disableEntry?.(selectedEntry) && !selectedSense)} on:click={onPick}>
      <slot name="submit-text">
        Select {selectedSense ? 'Sense' : 'Entry'}
      </slot>
    </Button>
  </div>
</Dialog>
