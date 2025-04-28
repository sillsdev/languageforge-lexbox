<script lang="ts" context="module">
  import type {IEntry, ISense} from '$lib/dotnet-types';

  export type EntrySenseSelection = {
    entry: IEntry;
    sense?: ISense;
  };
</script>

<script lang="ts">
  import {mdiBookPlusOutline, mdiBookSearchOutline, mdiMagnifyRemoveOutline, mdiPlus} from '@mdi/js';
  import {Icon as UxIcon, ListItem, ProgressCircle} from 'svelte-ux';
  import {getContext} from 'svelte';
  import {useLexboxApi} from '../services/service-provider';
  import {cn, defaultSense} from '../utils';
  import {useProjectCommands} from '../commands';
  import type {SaveHandler} from '../services/save-event-service';
  import {SortField} from '$lib/dotnet-types';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import NewEntryButton from './NewEntryButton.svelte';
  import {resource} from 'runed';
  import {Accordion} from "bits-ui";
  import {Button} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {ComposableInput} from '$lib/components/ui/input';
  import {Icon} from '$lib/components/ui/icon';
  import EntryRow from '../../project/browse/EntryRow.svelte';

  const projectCommands = useProjectCommands();
  const saveHandler = getContext<SaveHandler>('saveHandler');
  const writingSystemService = useWritingSystemService();

  interface Props {
    open?: boolean;
    title: string;
    disableEntry?: ((entry: IEntry) => false | { reason: string, disableSenses?: true }) | undefined;
    disableSense?: ((sense: ISense, entry: IEntry) => false | string) | undefined;
    mode?: 'entries-and-senses' | 'only-entries';
    pick?: (selection: EntrySenseSelection) => void;
  }

  let {
    open = $bindable(false),
    title,
    disableEntry = undefined,
    disableSense = undefined,
    mode = 'entries-and-senses',
    pick = undefined,
  }: Props = $props();
  let onlyEntries = $derived(mode === 'only-entries');

  let selectedEntry: IEntry | undefined = $state(undefined);
  let selectedSense: ISense | undefined = $state(undefined);
  // We need this redundant field so the ExpandPanel has something to bind to. Otherwise it's very fragile.
  // So it's basically just for managing the state of the ExpansionPanel.
  let selectedEntryId: string | undefined = $state(undefined);

  const lexboxApi = useLexboxApi();
  let search = $state('')
  const fetchCount = 150;
  const displayCount = 50;

  let addedEntries: IEntry[] = $state([]);
  const searchResource = resource(() => search, (search) => {
    if (!search) return [];
    return lexboxApi.searchEntries(search ?? '', {
      offset: 0,
      count: fetchCount,
      order: {field: SortField.Headword, writingSystem: 'default', ascending: true},
    });
  }, {initialValue: []});
  const displayedEntries = $derived(searchResource.current?.slice(0, displayCount) ?? []);
  $effect(() => {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    searchResource.current;
    addedEntries = [];
  });

  function onPick() {
    pick?.({entry: selectedEntry!, sense: selectedSense});
    open = false;
  }

  $effect(() => {
    if (!open) {
      reset();
    }
  });

  function reset() {
    search = '';
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
    const entry = await projectCommands.createNewEntry(search, {dontSelect: true});
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
  $effect(() => {
    if (!selectedEntryId) {
      select()
      return;
    }
    let entry = displayedEntries.find(e => e.id === selectedEntryId);
    if (disableEntry && entry && disableEntry(entry)) entry = undefined;
    select(entry);
  });

  function onExpansionClick(disableExpand: boolean, entry: IEntry) {
    console.log('expansion clicked', disableExpand);
    if (disableExpand) {
      // In this case, the ExpansionPanel' on:change event above is not in use, so we need to manage state here
      select(selectedEntry?.id === entry.id ? undefined : entry);
    }
  }
</script>

<Dialog.Root bind:open>
  <Dialog.Content class="pb-0">
    <Dialog.Header>
      <Dialog.Title class="mb-4">
        {title}
      </Dialog.Title>
      <ComposableInput bind:value={search} placeholder="Find entry..." autofocus class="px-1">
        {#snippet before()}
          <Icon icon="i-mdi-book-search-outline"/>
        {/snippet}
        {#snippet after()}
          {#if searchResource.loading}
            <ProgressCircle size={20} width={2}/>
          {/if}
        {/snippet}
      </ComposableInput>
    </Dialog.Header>

    <div class="p-1">
      {#if onlyEntries}
        <div class="space-y-2">
          {#each [...displayedEntries, ...addedEntries] as entry (entry.id)}
            {@const disabledEntry = disableEntry?.(entry)}
            <EntryRow {entry} isSelected={selectedEntry === entry} onclick={() => select(entry)}>
              {#snippet badge()}
                {#if disabledEntry}
                    <span
                      class="mr-2 shrink-0 h-7 px-2 justify-center inline-flex items-center border border-warning text-warning rounded-lg">
                      {disabledEntry.reason}
                    </span>
                {/if}
              {/snippet}
            </EntryRow>
          {/each}
        </div>
      {:else}
        <Accordion.Root type="single" bind:value={selectedEntryId}>
        {#each [...displayedEntries, ...addedEntries] as entry (entry.id)}
          {@const disabledEntry = disableEntry?.(entry)}
          {@const disableExpand = !!(disabledEntry && disabledEntry.disableSenses)}
          <Accordion.Item value={entry.id} class="data-[state=open]:border">
            <Accordion.Header class={cn('hover:bg-accent p-2', entry.id === selectedEntryId && !selectedSense && 'bg-accent')} onclick={() => onExpansionClick(disableExpand, entry)}>
              <Accordion.Trigger class="w-full flex" disabled={!!disabledEntry}>
                <div class="flex flex-col items-start">
                  <p class="font-medium text-xl">{writingSystemService.headword(entry).padStart(1, '–')}</p>
                  <p class="text-muted-foreground">{writingSystemService.glosses(entry).padStart(1, '–')}</p>
                </div>
                <div class="grow"></div>
                {#if disabledEntry}
                  <span
                    class="mr-2 shrink-0 h-7 px-2 justify-center inline-flex items-center border border-warning text-warning rounded-lg">
                    {disabledEntry.reason}
                  </span>
                {/if}
                {#if entry.senses.length}
                  <span
                    class="aspect-square size-7 mr-4 shrink-0 justify-center inline-flex items-center border border-info text-info rounded-lg">
                    {entry.senses.length}
                  </span>
                {/if}
              </Accordion.Trigger>
            </Accordion.Header>
            {#if !disableExpand}
              <Accordion.Content>
                <p class="text-muted-foreground p-2 text-sm">Senses:</p>
                {#each entry.senses as sense}
                  {@const disabledSense = disableSense?.(sense, entry)}
                  <button
                    class="sense w-full flex-1 flex justify-between items-center text-left max-w-full overflow-hidden hover:bg-accent p-2 pl-4"
                    class:bg-accent={selectedSense?.id === sense.id}
                    class:disabled={disabledSense}
                    onclick={() => select(entry, sense)}>
                    <div class="flex flex-col items-start">
                      <p class="font-medium text-xl">{writingSystemService.firstGloss(sense).padStart(1, '–')}</p>
                      <p class="text-muted-foreground">{writingSystemService.firstDef(sense).padStart(1, '–')}</p>
                    </div>
                    {#if disabledSense}
                      <span
                        class="mr-4 shrink-0 h-7 px-2 justify-center inline-flex items-center border border-warning text-warning rounded-lg">
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
              </Accordion.Content>
            {/if}
          </Accordion.Item>
        {/each}
      </Accordion.Root>
      {/if}
      {#if displayedEntries.length === 0 && addedEntries.length === 0}
        <div class="p-4 text-center opacity-75 flex justify-center items-center gap-2">
          {#if search}
            No entries found <UxIcon data={mdiMagnifyRemoveOutline} />
            <NewEntryButton on:click={onClickCreateNewEntry} />
          {:else if searchResource.loading}
            <ProgressCircle size={30} />
          {:else}
              Search for an entry {onlyEntries ? '' : 'or sense'} <UxIcon data={mdiBookSearchOutline} /> or
              <NewEntryButton on:click={onClickCreateNewEntry} />
          {/if}
        </div>
      {/if}
      {#if displayedEntries.length}
        <ListItem
          title="Create new Entry..."
          icon={mdiBookPlusOutline}
          classes={{root: 'text-success py-4 border-none rounded m-0.5 hover:bg-success-900/25'}}
          noShadow
          on:click={onClickCreateNewEntry}
        />
      {/if}
      {#if searchResource.current.length > displayedEntries.length}
        <div class="px-4 py-2 text-center opacity-75 flex items-center">
          <span>{searchResource.current.length - displayedEntries.length}</span>
          {#if searchResource.current.length === fetchCount}<span>+</span>{/if}
          <div class="ml-1 flex justify-between items-center gap-2">
            <span>more matching entries...</span>
          </div>
        </div>
      {/if}
    </div>

    <Dialog.Footer class="sticky bottom-0 pointer-events-none">
      <div class="flex gap-4 items-end p-2 pb-4 bg-background rounded flex-nowrap pointer-events-auto">
        <Button variant="secondary" class="basis-1/4" onclick={() => open = false}>
          Cancel
        </Button>
        <Button variant="default" class="basis-3/4"
                disabled={!selectedEntry || (disableEntry && !!disableEntry(selectedEntry) && !selectedSense)}
                onclick={onPick}>
          Select {selectedSense ? 'Sense' : 'Entry'}
        </Button>
      </div>

    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
