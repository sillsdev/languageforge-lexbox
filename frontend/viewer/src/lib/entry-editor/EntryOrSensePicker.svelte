<script lang="ts" module>
  import type {IEntry, ISense} from '$lib/dotnet-types';

  export type EntrySenseSelection = {
    entry: IEntry;
    sense?: ISense;
  };
</script>

<script lang="ts">
  import {useLexboxApi} from '../services/service-provider';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {SortField} from '$lib/dotnet-types';
  import NewEntryButton from './NewEntryButton.svelte';
  import {resource, watch} from 'runed';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import * as Dialog from '$lib/components/ui/dialog';
  import {ComposableInput} from '$lib/components/ui/input';
  import {Icon} from '$lib/components/ui/icon';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import SubmitOrCancel from '$lib/components/SubmitOrCancel.svelte';
  import EntryRow from '../../project/browse/EntryRow.svelte';
  import SenseRow from '../../project/browse/SenseRow.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import {t, T} from 'svelte-i18n-lingui';
  import ListItem from '$lib/components/ListItem.svelte';
  import type {DialogTriggerProps} from 'bits-ui';
  import {Badge} from '$lib/components/ui/badge';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import {DEFAULT_DEBOUNCE_TIME} from '$lib/utils/time';
  import {cn} from '$lib/utils';
  import {useWritingSystemService} from '$project/data';

  const currentView = useCurrentView();
  const writingSystemService = useWritingSystemService();
  const dialogsService = useDialogsService();

  interface Props {
    open?: boolean;
    title: string;
    disableEntry?: ((entry: IEntry) => false | { reason: string, disableSenses?: true }) | undefined;
    disableSense?: ((sense: ISense, entry: IEntry) => false | string) | undefined;
    mode?: 'entries-and-senses' | 'only-entries';
    pick?: (selection: EntrySenseSelection) => void;
    trigger?: DialogTriggerProps['child'];
  }

  let {
    open = $bindable(false),
    title,
    disableEntry,
    disableSense,
    mode = 'entries-and-senses',
    pick,
    trigger,
  }: Props = $props();
  let onlyEntries = $derived(mode === 'only-entries');

  let selectedEntry: IEntry | undefined = $state(undefined);
  let selectedSense: ISense | undefined = $state(undefined);
  let isEntryOrSenseOpen = $state(false);
  const collapsedSelectionPreview = $derived.by(() => {
    if (!selectedEntry) return '';
    if (!selectedSense) {
      return writingSystemService.headword(selectedEntry) || '';
    }
    return writingSystemService.firstGloss(selectedSense) || writingSystemService.firstDef(selectedSense) || '';
  });

  const lexboxApi = useLexboxApi();
  let search = $state('');
  const PAGE_SIZE = 50;
  let displayCount = $state(PAGE_SIZE);
  let fetchCount = $state(PAGE_SIZE * 2);

  let addedEntries: IEntry[] = $state([]);
  const searchResource = resource([() => search, () => fetchCount], () => {
    if (!search) return Promise.resolve([]);
    return lexboxApi.searchEntries(search ?? '', {
      offset: 0,
      count: fetchCount,
      order: {field: SortField.SearchRelevance, writingSystem: 'default', ascending: true},
    });
  }, {initialValue: [], debounce: DEFAULT_DEBOUNCE_TIME});
  const displayedEntries = $derived(searchResource.current?.slice(0, displayCount) ?? []);
  $effect(() => {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    searchResource.current;
    addedEntries = [];
  });

  watch([() => open, () => search], ([open]) => {
    if (open) {
      displayCount = PAGE_SIZE;
      fetchCount = PAGE_SIZE * 2;
    }
  });

  function showMoreEntries() {
    displayCount += PAGE_SIZE;
    fetchCount += PAGE_SIZE;
  }

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
    selectedSense = undefined;
    addedEntries = [];
  }

  // async function onClickAddSense(entry: IEntry): Promise<void> {
  //   const newSense = defaultSense(entry.id);
  //   const savedSense = await saveHandler.handleSave(() => lexboxApi.createSense(entry.id, newSense));
  //   entry.senses = [...entry.senses, savedSense];
  //   selectedSense = savedSense;
  //   onPick();
  //   open = false;
  // }


  async function onClickCreateNewEntry(): Promise<void> {
    const entry = await dialogsService.createNewEntry(search);
    selectedEntry = entry;
    if (entry) {
      addedEntries = [entry];
    }
  }

  function select(entry?: IEntry, sense?: ISense): void {
    selectedEntry = entry;
    selectedSense = sense;
    isEntryOrSenseOpen = true;
  }
</script>

<Dialog.Root bind:open>
  <Dialog.Trigger child={trigger} />
  <Dialog.Content class="pb-0 @container sm:min-h-[min(calc(100%-16px),30rem)]" style="grid-template-rows: auto 1fr auto">
    <Dialog.Header>
      <Dialog.Title class="mb-4">
        {title}
      </Dialog.Title>
      <ComposableInput bind:value={search} placeholder={pt($t`Find entry...`, $t`Find word...`, $currentView)} autofocus class="px-1">
        {#snippet before()}
          <Icon icon="i-mdi-book-search-outline"/>
        {/snippet}
        {#snippet after()}
          {#if searchResource.loading}
            <Loading/>
          {/if}
        {/snippet}
      </ComposableInput>
    </Dialog.Header>

    <div class="space-y-2">
      {#each [...displayedEntries, ...addedEntries] as entry (entry.id)}
        {@const disabledEntry = disableEntry?.(entry)}
        <EntryRow {entry} disabled={disabledEntry && disabledEntry.disableSenses} selected={selectedEntry === entry} onclick={() => select(entry)}>
          {#snippet badge()}
            {#if disabledEntry}
              <Badge variant="outline" class="border-destructive text-destructive">
                {disabledEntry.reason}
              </Badge>
            {/if}
          {/snippet}
        </EntryRow>
      {:else}
        {#if searchResource.loading}
          <EntryRow skeleton/>
          <EntryRow skeleton/>
          <EntryRow skeleton/>
          <EntryRow skeleton/>
          <EntryRow skeleton/>
        {:else}
          <div class="p-4 text-center opacity-75 flex justify-center items-center gap-2">
            {#if search}
              {pt($t`No entries found`, $t`No words found`, $currentView)}
              <Icon icon="i-mdi-magnify-remove-outline"/>
              <NewEntryButton onclick={onClickCreateNewEntry}/>
            {:else}
              <T msg="Search # or #">
                <Icon icon="i-mdi-book-search-outline"/>
                {#snippet second()}
                  <NewEntryButton onclick={onClickCreateNewEntry}/>
                {/snippet}
              </T>
            {/if}
          </div>
        {/if}
      {/each}
      {#if searchResource.current.length > displayedEntries.length}
        {@const remainingEntries = searchResource.current.length - displayedEntries.length}
        <Button class="w-full h-14" variant="outline" onclick={showMoreEntries}>
          {$t`Show ${remainingEntries} more...`}
        </Button>
      {/if}
      {#if displayedEntries.length}
        <NewEntryButton onclick={onClickCreateNewEntry} class="w-full h-14 bg-primary/95" variant="default"/>
      {/if}
    </div>

    <Dialog.Footer class="sticky bottom-0 gap-0 flex-col! items-stretch! bg-background border rounded rounded-b-none border-b-0 scale-[1.02] overflow-x-hidden h-fit">
      {#if !onlyEntries && selectedEntry}
        {@const disabledEntry = disableEntry?.(selectedEntry)}
        <div class="pointer-events-auto px-2 pt-2 max-w-full max-h-[min(calc(100cqh-13rem),20rem)] overflow-y-auto overscroll-contain">
          <Collapsible.Root bind:open={isEntryOrSenseOpen}>
            <Collapsible.Trigger class={cn(buttonVariants({variant: 'ghost', size: 'sm'}), 'w-full justify-between text-muted-foreground')}>
              <span class="flex items-center gap-1.5 truncate">
                <span>{pt($t`Entry or sense:`, $t`Word or meaning:`, $currentView)}</span>
                <span class="text-foreground truncate">
                  {collapsedSelectionPreview}
                </span>
              </span>
              <Icon icon={isEntryOrSenseOpen ? 'i-mdi-chevron-down' : 'i-mdi-chevron-up'} class="size-5"/>
            </Collapsible.Trigger>
            <Collapsible.Content class="space-y-2 py-2.5">
              <ListItem
                class="flex-row justify-between"
                disabled={!!disableEntry?.(selectedEntry)}
                selected={!selectedSense}
                onclick={() => select(selectedEntry, undefined)}>
                <p class="font-medium">{pt($t`Entry Only`, $t`Word only`, $currentView)}</p>
                {#if disabledEntry}
                  <Badge variant="outline" class="border-destructive text-destructive">
                    {disabledEntry.reason}
                  </Badge>
                {/if}
              </ListItem>
              {#each selectedEntry.senses as sense (sense.id)}
                {@const disabledSense = disableSense?.(sense, selectedEntry)}
                <SenseRow
                  {sense}
                  selected={selectedSense?.id === sense.id}
                  disabled={!!disabledSense}
                  onclick={() => select(selectedEntry, sense)}>
                  {#snippet badge()}
                    {#if disabledSense}
                      <Badge variant="outline" class="border-destructive text-destructive">
                        {disabledSense}
                      </Badge>
                    {/if}
                  {/snippet}
                </SenseRow>
              {/each}
            </Collapsible.Content>
          </Collapsible.Root>
<!--          disabled for now because this didn't prompt the user to define the sense, it just created it with no data-->
<!--          <button
            class="w-full flex-1 flex items-center text-left max-w-full overflow-hidden hover:bg-accent p-2 pl-4 rounded"
            onclick={() => onClickAddSense(selectedEntry)}>
            <Icon icon="i-mdi-plus"/>
            <p class="font-medium text-xl">Add sense...</p>
          </button>-->
        </div>
      {/if}
      <SubmitOrCancel
        canSubmit={!!(selectedEntry && (!disableEntry || !disableEntry(selectedEntry) || selectedSense))}
        onCancel={() => open = false}
        onSubmit={onPick}
        submitLabel={$t`Select ${selectedSense ? pt($t`Sense`, $t`Meaning`, $currentView) : pt($t`Entry`, $t`Word`, $currentView)}`}
        class="p-4"
      />

    </Dialog.Footer>
  </Dialog.Content>
</Dialog.Root>
