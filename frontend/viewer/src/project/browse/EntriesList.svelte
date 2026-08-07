<script lang="ts">
  import {type IEntry, type IPartOfSpeech, type IPublication, type ISemanticDomain} from '$lib/dotnet-types';
  import {Debounced, watch} from 'runed';
  import EntryRow from './EntryRow.svelte';
  import Button from '$lib/components/ui/button/button.svelte';
  import ListItem from '$lib/components/ListItem.svelte';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import DevContent from '$lib/layout/DevContent.svelte';
  import PrimaryNewEntryButton from '../PrimaryNewEntryButton.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import EntryMenu from './EntryMenu.svelte';
  import FabContainer from '$lib/components/fab/fab-container.svelte';
  import {VList, type VListHandle} from 'virtua/svelte';
  import type {SortConfig} from './sort/options';
  import {AppNotification} from '$lib/notifications/notifications';
  import {Icon} from '$lib/components/ui/icon';
  import {useProjectContext} from '$project/project-context.svelte';
  import Delayed from '$lib/components/Delayed.svelte';
  import {EntryLoaderService} from '$lib/services/entry-loader-service.svelte';
  import {onDestroy, untrack} from 'svelte';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {pt} from '$lib/views/view-text';

  let {
    search = '',
    selectedEntryId = undefined,
    publication = undefined,
    partOfSpeech = undefined,
    semanticDomain = undefined,
    sort,
    onSelectEntry,
    gridifyFilter = undefined,
    previewDictionary = false,
    disableNewEntry = false,
    entryCount = $bindable(null),
  }: {
    search?: string;
    selectedEntryId?: string;
    publication?: IPublication;
    partOfSpeech?: IPartOfSpeech;
    semanticDomain?: ISemanticDomain;
    sort?: SortConfig;
    onSelectEntry: (entry?: IEntry) => void;
    gridifyFilter?: string;
    previewDictionary?: boolean,
    disableNewEntry?: boolean,
    entryCount?: number | null,
  } = $props();
  const projectContext = useProjectContext();
  const miniLcmApi = $derived(projectContext.maybeApi);
  const dialogsService = useDialogsService();
  const projectEventBus = useProjectEventBus();
  const viewService = useViewService();

  // The closures maybe need to be created OUTSIDE untrack so they maintain reactivity
  const deps = {
    search: () => search,
    sort: () => sort,
    gridifyFilter: () => gridifyFilter,
  };

  let entryLoader = $derived(!miniLcmApi ? undefined : untrack(() => new EntryLoaderService(miniLcmApi, deps)));
  const displayedEntryCount = $derived(entryLoader?.totalCount ?? 0);

  // Destroy the previous entryLoader when a new one is created
  watch(
    () => entryLoader,
    (_current, previous) => previous?.destroy()
  );

  onDestroy(() => entryLoader?.destroy());

  // Debounce the loading state for smoother UI
  const loading = new Debounced(() => entryLoader?.loading ?? true, 0);

  // Keep entryCount in sync
  $effect(() => {
    entryCount = entryLoader?.totalCount ?? null;
  });

  // A change can move an entry in the sort order or in/out of the current filter,
  // so we re-query the visible range once rather than patch rows per entry.
  projectEventBus.onEntriesChanged(({changedEntryIds}) => {
    void entryLoader?.quietReset().then(() => {
      // follow the selected entry if it "jumps"/reorders
      if (selectedEntryId && changedEntryIds.includes(selectedEntryId)) {
        return tryToScrollToEntry(selectedEntryId);
      }
    });
  });

  $effect(() => {
    if (entryLoader?.error) {
      AppNotification.error($t`Failed to load entries`, entryLoader.error.message);
    }
  });

  // Generate a random number of skeleton rows
  const skeletonRowCount = Math.ceil(Math.random() * 3) + 3;

  const canCreateFromSearch = $derived(search?.trim() && !disableNewEntry);
  const showTerminalCreateRow = $derived(canCreateFromSearch && displayedEntryCount > 0);

  type ListRow = {key: string, index: number, create?: boolean};
  const rows: ListRow[] = $derived.by(() => {
    if (entryLoader?.totalCount === undefined) {
      return Array.from({length: skeletonRowCount}, (_, i) => ({key: `skeleton-${i}`, index: i}));
    }
    const generation = entryLoader.generation;
    const entryRows: ListRow[] = Array.from({length: displayedEntryCount}, (_, i) => ({key: `${generation}-${i}`, index: i}));
    if (showTerminalCreateRow) {
      entryRows.push({key: `${generation}-create`, index: displayedEntryCount, create: true});
    }
    return entryRows;
  });

  async function handleNewEntry(headword: string | undefined = undefined) {
    const entry = await dialogsService.createNewEntry(headword, {
      publishIn: publication ? [publication] : [],
    }, {
      semanticDomains: semanticDomain ? [semanticDomain] : [],
      partOfSpeech: partOfSpeech,
    });
    if (!entry) return;
    onSelectEntry(entry);
  }

  let vList = $state<VListHandle>();
  watch(() => [vList, selectedEntryId, entryLoader?.loading], () => {
    if (!vList || entryLoader?.loading !== false) return;
    void scrollToSelectedOrTop();
  });

  async function scrollToSelectedOrTop() {
    const currentId = selectedEntryId;
    if (!vList) return;
    const scrolled = currentId && await tryToScrollToEntry(currentId);
    if (!scrolled && selectedEntryId === currentId) vList.scrollTo(0);
  }

  export async function tryToScrollToEntry(entryId: string): Promise<boolean> {
    if (!entryLoader || !vList) return false;

    const index = await entryLoader.getOrLoadEntryIndex(entryId);
    if (entryId !== selectedEntryId) return false;
    if (index < 0) return false;
    if (!vList) return false; // may have unmounted during the await

    const visibleStart = vList.getScrollOffset();
    const visibleSize = vList.getViewportSize();
    const visibleEnd = visibleStart + visibleSize;
    const itemStart = vList.getItemOffset(index);
    const itemSize = vList.getItemSize(index);
    const itemEnd = itemStart + itemSize;

    if (itemStart < visibleStart || itemEnd > visibleEnd) {
      //using smooth scroll caused lag, maybe only do it if scrolling a short distance?
      vList.scrollToIndex(index, { align: 'center' });
    }
    return true;
  }

  export async function selectNextEntry(): Promise<IEntry | undefined> {
    if (!entryLoader) return undefined;
    const indexOfSelected = selectedEntryId
      ? await entryLoader.getOrLoadEntryIndex(selectedEntryId)
      : -1;
    const nextIndex = indexOfSelected < 0 ? 0 : indexOfSelected + 1;

    // Check count bounds
    if (entryLoader.totalCount === undefined || nextIndex >= entryLoader.totalCount) {
      return undefined;
    }

    const nextEntry = await entryLoader.getOrLoadEntryByIndex(nextIndex);
    onSelectEntry(nextEntry);
    return nextEntry;
  }

</script>

{#snippet newEntryFromSearchRow(className: string = '')}
  <ListItem
    class={cn('bg-transparent shadow-none hover:shadow-none border-2 border-dashed border-muted-foreground/40', className)}
    onclick={() => handleNewEntry(search)}>
    {#snippet icon()}
      <Icon icon="i-mdi-plus-thick" class="size-6 text-primary/60" />
    {/snippet}
    <span class="font-medium text-2xl">{search}</span>
    <span class="text-sm text-muted-foreground">{$t`Add to dictionary`}</span>
  </ListItem>
{/snippet}

<FabContainer>
  <DevContent>
    <Button
      icon={loading.current ? 'i-mdi-loading' : 'i-mdi-refresh'}
      variant="outline"
      iconProps={{ class: cn(loading.current && 'animate-spin') }}
      size="icon"
      onclick={() => entryLoader?.reset()}
    />
  </DevContent>
  {#if !disableNewEntry}
    <PrimaryNewEntryButton onclick={() => handleNewEntry()} shortForm />
  {/if}
</FabContainer>

<div class="flex-1 h-full" role="table">
  {#if entryLoader?.error}
    <div class="flex items-center justify-center h-full text-muted-foreground gap-2">
      <Icon icon="i-mdi-alert-circle-outline" />
      <p>{$t`Failed to load entries`}</p>
    </div>
  {:else}
    <div class="h-full">
      {#if entryLoader?.totalCount === 0}
        <div class="flex flex-col items-center justify-center h-full gap-4 px-4">
          <p class="text-muted-foreground">{pt($t`No entries found`, $t`No words found`, viewService.currentView)}</p>

          {#if canCreateFromSearch}
            {@render newEntryFromSearchRow('max-w-md')}
          {/if}
        </div>
      {/if}

      <VList bind:this={vList}
            data={rows}
            class="h-full p-0.5 md:pr-3 after:h-12 after:block"
            getKey={(row: ListRow) => row.key}
            bufferSize={450}>
        {#snippet children(row: ListRow)}
          {#if row.create}
            {@render newEntryFromSearchRow()}
          {:else}
            <Delayed
              getCached={() => entryLoader?.getCachedEntryByIndex(row.index)}
              load={() => entryLoader?.getOrLoadEntryByIndex(row.index)}
              delay={250}
            >
              {#snippet children(state)}
                {#if state.loading || !state.current}
                  <EntryRow class="mb-2" skeleton={true}/>
                {:else}
                  {@const entry = state.current}
                  <EntryMenu {entry} contextMenu>
                    <EntryRow {entry}
                              class="mb-2"
                              selected={selectedEntryId === entry.id}
                              onclick={() => onSelectEntry(entry)}
                              {previewDictionary}/>
                  </EntryMenu>
                {/if}
              {/snippet}
            </Delayed>
          {/if}
        {/snippet}
      </VList>
    </div>
  {/if}
</div>
