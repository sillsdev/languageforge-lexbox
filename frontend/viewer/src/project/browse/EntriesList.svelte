<script lang="ts">
  import {MorphType, type IEntry, type IPartOfSpeech, type ISemanticDomain, type IEntriesWindow} from '$lib/dotnet-types';
  import type {IQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
  import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
  import {Debounced, resource, watch} from 'runed';
  import EntryRow from './EntryRow.svelte';
  import Button from '$lib/components/ui/button/button.svelte';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import DevContent from '$lib/layout/DevContent.svelte';
  import PrimaryNewEntryButton from '../PrimaryNewEntryButton.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import EntryMenu from './EntryMenu.svelte';
  import FabContainer from '$lib/components/fab/fab-container.svelte';
  import {VList, type VListHandle} from 'virtua/svelte';
  import type {SortConfig} from './SortMenu.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import {Icon} from '$lib/components/ui/icon';
  import {useProjectContext} from '$project/project-context.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {useCurrentView} from '$lib/views/view-service';

  const LITE_MORPHEME_TYPES = new Set([
    MorphType.Root, MorphType.BoundRoot,
    MorphType.Stem, MorphType.BoundStem,
    MorphType.Particle,
    MorphType.Phrase, MorphType.DiscontiguousPhrase,
  ]);

  // Page size for virtual scrolling
  const PAGE_SIZE = 100;
  // Buffer before edge to trigger loading more entries
  const LOAD_THRESHOLD = 20;

  function filterLiteMorphemeTypes(entries: IEntry[]): IEntry[] {
    // we could do this server-side, but doing it client-side provides better UX and presumably we'll
    // only ever filter out a small portion of entries
    const filteredEntries = entries.filter(entry =>
      entry.morphType && LITE_MORPHEME_TYPES.has(entry.morphType));
    const hiddenEntries = entries.length - filteredEntries.length;
    console.debug(`Filtered out ${hiddenEntries} non-wordy morpheme entries (for FW Lite view)`);
    return filteredEntries;
  }

  let {
    search = '',
    selectedEntryId = undefined,
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
  const currentView = useCurrentView();

  // Virtual scrolling state
  // entries currently loaded in memory (a sliding window)
  let loadedEntries = $state<IEntry[]>([]);
  // offset of the first loaded entry in the full list
  let windowOffset = $state(0);
  // total count of entries matching the current query/filter
  let totalCount = $state(0);
  // tracks if we're currently loading more entries (to prevent duplicate fetches)
  let isLoadingMore = $state(false);
  // tracks which direction we're loading (for proper state management)
  let loadingDirection = $state<'initial' | 'before' | 'after' | 'target'>('initial');

  projectEventBus.onEntryDeleted(entryId => {
    if (selectedEntryId === entryId) onSelectEntry(undefined);
    if (loadingUndebounced || !loadedEntries.some(e => e.id === entryId)) return;
    const currentIndex = loadedEntries.findIndex(e => e.id === entryId);
    if (currentIndex >= 0) {
      loadedEntries.splice(currentIndex, 1);
      totalCount = Math.max(0, totalCount - 1);
    }
  });
  projectEventBus.onEntryUpdated(_entry => {
    if (loadingUndebounced) return;
    const currentIndex = loadedEntries.findIndex(e => e.id === _entry.id);
    if (currentIndex >= 0) {
      loadedEntries[currentIndex] = _entry;
    } else {
      // Entry might be newly in range, do a silent refresh
      void silentlyRefreshEntries();
    }
  });

  async function silentlyRefreshEntries() {
    if (!miniLcmApi) return;
    const window = await fetchEntriesWindow(windowOffset, PAGE_SIZE);
    if (window) {
      loadedEntries = window.entries;
      totalCount = window.totalCount;
    }
  }

  let loadingUndebounced = $state(true);
  const loading = new Debounced(() => loadingUndebounced, 50);

  function buildQueryOptions(offset: number, count: number): IQueryOptions {
    return {
      count,
      offset,
      filter: {
        gridifyFilter: gridifyFilter ? gridifyFilter : undefined,
      },
      order: {
        field: sort?.field ?? SortField.SearchRelevance,
        writingSystem: 'default',
        ascending: sort?.dir !== 'desc',
      },
    };
  }

  async function fetchEntriesWindow(offset: number, count: number, targetEntryId?: string): Promise<IEntriesWindow | null> {
    if (!miniLcmApi) return null;
    const queryOptions = buildQueryOptions(offset, count);
    return miniLcmApi.getEntriesWindow(search || undefined, queryOptions, targetEntryId);
  }

  // Initial load: fetch first page, or center on selected entry if provided
  async function loadInitialEntries(targetEntryId?: string): Promise<IEntriesWindow | null> {
    if (!miniLcmApi) return null;
    loadingUndebounced = true;
    loadingDirection = targetEntryId ? 'target' : 'initial';

    try {
      // If we have a target entry, request entries centered around it
      // The API will return targetIndex telling us where the entry is in the result
      const window = await fetchEntriesWindow(0, PAGE_SIZE, targetEntryId);
      if (!window) return null;

      // API returns entries starting from an offset that includes the target entry
      windowOffset = window.offset;
      loadedEntries = window.entries;
      totalCount = window.totalCount;

      return window;
    } finally {
      loadingUndebounced = false;
      loadingDirection = 'initial';
    }
  }

  // Load more entries before the current window
  async function loadEntriesBefore(): Promise<void> {
    if (isLoadingMore || windowOffset <= 0) return;
    isLoadingMore = true;
    loadingDirection = 'before';

    try {
      const newOffset = Math.max(0, windowOffset - PAGE_SIZE);
      const countToLoad = windowOffset - newOffset;
      if (countToLoad <= 0) return;

      const window = await fetchEntriesWindow(newOffset, countToLoad);
      if (!window || window.entries.length === 0) return;

      // Prepend new entries
      loadedEntries = [...window.entries, ...loadedEntries];
      windowOffset = newOffset;
      totalCount = window.totalCount;
    } finally {
      isLoadingMore = false;
      loadingDirection = 'initial';
    }
  }

  // Load more entries after the current window
  async function loadEntriesAfter(): Promise<void> {
    const currentEnd = windowOffset + loadedEntries.length;
    if (isLoadingMore || currentEnd >= totalCount) return;
    isLoadingMore = true;
    loadingDirection = 'after';

    try {
      const window = await fetchEntriesWindow(currentEnd, PAGE_SIZE);
      if (!window || window.entries.length === 0) return;

      // Append new entries
      loadedEntries = [...loadedEntries, ...window.entries];
      totalCount = window.totalCount;
    } finally {
      isLoadingMore = false;
      loadingDirection = 'initial';
    }
  }

  // Triggered when a new search/sort/filter is applied
  // Note: We track initialSelectedEntryId separately to avoid refetching when user selects a different entry
  // eslint-disable-next-line svelte/valid-compile -- intentionally capturing initial value
  let initialSelectedEntryId = $state(selectedEntryId);
  
  // Update initialSelectedEntryId only when search/sort/filter changes
  $effect(() => {
    // When search/sort/filter changes, reset to current selection
    void search; void sort; void gridifyFilter;
    initialSelectedEntryId = selectedEntryId;
  });

  const entriesResource = resource(
    () => ({ search, sort, gridifyFilter, miniLcmApi }),
    async (_curr, _prev, refetchInfo): Promise<IEntry[]> => {
      // On dependency change, load initial entries (centering on selected if available)
      const window = await loadInitialEntries(initialSelectedEntryId);
      if (refetchInfo.signal.aborted) return loadedEntries;

      // If we got a targetIndex, scroll to it after render
      if (window?.targetIndex !== undefined && window.targetIndex !== null) {
        // targetIndex is relative to the start of the returned window
        pendingScrollToIndex = window.targetIndex;
      }

      return window?.entries ?? [];
    });

  const entries = $derived.by(() => {
    let currEntries = loadedEntries;
    if (currEntries.length && $currentView.type === 'fw-lite') {
      currEntries = filterLiteMorphemeTypes(currEntries);
    }
    return currEntries;
  });

  // Convert global entry index to local array index
  function globalToLocalIndex(globalIndex: number): number {
    return globalIndex - windowOffset;
  }

  // Check if we need to load more entries based on visible range
  function handleVisibleRangeChange(startIndex: number, endIndex: number): void {
    const localStart = startIndex;
    const localEnd = endIndex;

    // Check if we need to load entries before
    if (localStart < LOAD_THRESHOLD && windowOffset > 0 && !isLoadingMore) {
      void loadEntriesBefore();
    }

    // Check if we need to load entries after
    const remainingAfter = loadedEntries.length - localEnd;
    const globalEnd = windowOffset + loadedEntries.length;
    if (remainingAfter < LOAD_THRESHOLD && globalEnd < totalCount && !isLoadingMore) {
      void loadEntriesAfter();
    }
  }

  watch(() => [entries, loadingUndebounced], () => {
    if (!loadingUndebounced)
      entryCount = totalCount;
  });

  $effect(() => {
    if (entriesResource.error) {
      AppNotification.error($t`Failed to load entries`, entriesResource.error.message);
    }
  });

  // Generate a random number of skeleton rows between 3 and 7
  const skeletonRowCount = Math.floor(Math.random() * 5) + 3;

  async function handleNewEntry() {
    const entry = await dialogsService.createNewEntry(undefined, {
      semanticDomains: semanticDomain ? [semanticDomain] : [],
      partOfSpeech: partOfSpeech,
    });
    if (!entry) return;
    onSelectEntry(entry);
  }

  let vList = $state<VListHandle>();

  // Handle scroll events to trigger loading more entries
  function handleScroll(): void {
    if (!vList) return;
    const scrollOffset = vList.getScrollOffset();
    const viewportSize = vList.getViewportSize();
    // Find visible range using scroll position
    const startIndex = vList.findItemIndex(scrollOffset);
    const endIndex = vList.findItemIndex(scrollOffset + viewportSize);
    handleVisibleRangeChange(startIndex, endIndex);
  }

  // Scroll to selected entry after initial load
  // The API returns targetIndex which tells us where in the loaded window the entry is
  let pendingScrollToIndex = $state<number | null>(null);

  $effect(() => {
    if (pendingScrollToIndex !== null && vList && !loading.current) {
      vList.scrollToIndex(pendingScrollToIndex, {align: 'center'});
      pendingScrollToIndex = null;
    }
  });

  // When selectedEntryId changes and entry is already loaded, scroll to it
  // If not loaded, fetch a window centered around that entry
  $effect(() => {
    if (!vList || !selectedEntryId || loading.current || isLoadingMore) return;
    const indexOfSelected = entries.findIndex(e => e.id === selectedEntryId);
    
    if (indexOfSelected === -1) {
      // Entry not in current window - need to load entries around it
      void loadEntriesAroundEntry(selectedEntryId);
      return;
    }
    
    const visibleStart = vList.getScrollOffset();
    const visibleSize = vList.getViewportSize();
    const visibleEnd = visibleStart + visibleSize;
    const itemStart = vList.getItemOffset(indexOfSelected);
    const itemSize = vList.getItemSize(indexOfSelected);
    const itemEnd = itemStart + itemSize;
    if (itemStart < visibleStart || itemEnd > visibleEnd) {
      //using smooth scroll caused lag, maybe only do it if scrolling a short distance?
      vList.scrollToIndex(indexOfSelected, {align: 'center'});
    }
  });

  // Load entries centered around a specific entry (when navigating to an entry not in current window)
  async function loadEntriesAroundEntry(entryId: string): Promise<void> {
    if (!miniLcmApi || isLoadingMore) return;
    isLoadingMore = true;
    loadingDirection = 'target';

    try {
      const window = await fetchEntriesWindow(0, PAGE_SIZE, entryId);
      if (!window) return;

      windowOffset = window.offset;
      loadedEntries = window.entries;
      totalCount = window.totalCount;

      // Scroll to the target entry after state updates
      if (window.targetIndex !== undefined && window.targetIndex !== null) {
        pendingScrollToIndex = window.targetIndex;
      }
    } finally {
      isLoadingMore = false;
      loadingDirection = 'initial';
    }
  }

  export function selectNextEntry() {
    const indexOfSelected = entries.findIndex(e => e.id === selectedEntryId);
    const nextIndex = indexOfSelected === -1 ? 0 : indexOfSelected + 1;

    // Check if next entry is within loaded window
    if (nextIndex < entries.length) {
      const nextEntry = entries[nextIndex];
      onSelectEntry(nextEntry);
      return nextEntry;
    }

    // Need to load more entries - trigger load and select first of new batch
    if (windowOffset + loadedEntries.length < totalCount) {
      void loadEntriesAfter().then(() => {
        if (nextIndex < entries.length) {
          const nextEntry = entries[nextIndex];
          onSelectEntry(nextEntry);
        }
      });
    }
    return undefined;
  }

</script>

<FabContainer>
  <DevContent>
    <Button
      icon={loading.current ? 'i-mdi-loading' : 'i-mdi-refresh'}
      variant="outline"
      iconProps={{ class: cn(loading.current && 'animate-spin') }}
      size="icon"
      onclick={() => entriesResource.refetch()}
    />
  </DevContent>
  {#if !disableNewEntry}
    <PrimaryNewEntryButton onclick={handleNewEntry} shortForm />
  {/if}
</FabContainer>

<div class="flex-1 h-full" role="table">
  {#if entriesResource.error}
    <div class="flex items-center justify-center h-full text-muted-foreground gap-2">
      <Icon icon="i-mdi-alert-circle-outline" />
      <p>{$t`Failed to load entries`}</p>
    </div>
  {:else}
    <div class="h-full">
      {#if loading.current}
        <div class="md:pr-3 p-0.5">
          <!-- Show skeleton rows while loading -->
          {#each { length: skeletonRowCount }, _index}
            <EntryRow class="mb-2" skeleton={true} />
          {/each}
        </div>
      {:else}
        <VList bind:this={vList} data={entries ?? []} class="h-full p-0.5 md:pr-3 after:h-12 after:block" getKey={d => d.id} onscroll={handleScroll}>
          {#snippet children(entry)}
            <EntryMenu {entry} contextMenu>
              <EntryRow {entry}
                        class="mb-2"
                        selected={selectedEntryId === entry.id}
                        onclick={() => onSelectEntry(entry)}
                        {previewDictionary}/>
            </EntryMenu>
          {/snippet}
        </VList>
        {#if entries.length === 0}
          <div class="flex items-center justify-center h-full text-muted-foreground">
            <p>{$t`No entries found`}</p>
          </div>
        {/if}
      {/if}
    </div>
  {/if}
</div>
