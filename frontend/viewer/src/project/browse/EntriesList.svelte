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
    const filteredEntries = entries.filter(entry =>
      entry.morphType && LITE_MORPHEME_TYPES.has(entry.morphType));
    const hiddenEntries = entries.length - filteredEntries.length;
    if (hiddenEntries > 0) {
      console.debug(`Filtered out ${hiddenEntries} non-wordy morpheme entries (for FW Lite view)`);
    }
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
  let loadedEntries = $state<IEntry[]>([]);
  let windowOffset = $state(0);
  let totalCount = $state(0);
  let isLoadingMore = $state(false);

  // Handle entry events
  projectEventBus.onEntryDeleted(entryId => {
    if (selectedEntryId === entryId) onSelectEntry(undefined);
    if (loadingUndebounced) return;
    const index = loadedEntries.findIndex(e => e.id === entryId);
    if (index >= 0) {
      loadedEntries.splice(index, 1);
      totalCount = Math.max(0, totalCount - 1);
    }
  });

  projectEventBus.onEntryUpdated(entry => {
    if (loadingUndebounced) return;
    const index = loadedEntries.findIndex(e => e.id === entry.id);
    if (index >= 0) {
      loadedEntries[index] = entry;
    }
  });

  let loadingUndebounced = $state(true);
  const loading = new Debounced(() => loadingUndebounced, 50);

  function buildQueryOptions(offset: number, count: number): IQueryOptions {
    return {
      count,
      offset,
      filter: { gridifyFilter: gridifyFilter || undefined },
      order: {
        field: sort?.field ?? SortField.Headword,
        writingSystem: 'default',
        ascending: sort?.dir !== 'desc',
      },
    };
  }

  async function fetchEntriesWindow(offset: number, count: number, targetEntryId?: string): Promise<IEntriesWindow | null> {
    if (!miniLcmApi) return null;
    try {
      return await miniLcmApi.getEntriesWindow(search || undefined, buildQueryOptions(offset, count), targetEntryId);
    } catch (e) {
      console.error('Failed to fetch entries window:', e);
      throw e;
    }
  }

  // Load more entries before current window
  async function loadEntriesBefore(): Promise<void> {
    if (isLoadingMore || windowOffset <= 0) return;
    isLoadingMore = true;
    try {
      const newOffset = Math.max(0, windowOffset - PAGE_SIZE);
      const countToLoad = windowOffset - newOffset;
      if (countToLoad <= 0) return;

      console.log(`[EntriesList] Loading BEFORE: offset=${newOffset}, count=${countToLoad}`);
      const window = await fetchEntriesWindow(newOffset, countToLoad);
      if (!window?.entries.length) return;

      console.log(`[EntriesList] Loaded BEFORE: ${window.entries.length} entries, new windowOffset=${newOffset}`);
      loadedEntries = [...window.entries, ...loadedEntries];
      windowOffset = newOffset;
      totalCount = window.totalCount;
    } finally {
      isLoadingMore = false;
    }
  }

  // Load more entries after current window
  async function loadEntriesAfter(): Promise<void> {
    const currentEnd = windowOffset + loadedEntries.length;
    if (isLoadingMore || currentEnd >= totalCount) return;
    isLoadingMore = true;
    try {
      console.log(`[EntriesList] Loading AFTER: offset=${currentEnd}, count=${PAGE_SIZE}`);
      const window = await fetchEntriesWindow(currentEnd, PAGE_SIZE);
      if (!window?.entries.length) return;

      console.log(`[EntriesList] Loaded AFTER: ${window.entries.length} entries, total loaded=${loadedEntries.length + window.entries.length}`);
      loadedEntries = [...loadedEntries, ...window.entries];
      totalCount = window.totalCount;
    } finally {
      isLoadingMore = false;
    }
  }

  // Track the entry ID to center on during initial load
  // This is captured when search/sort/filter changes, not when user clicks an entry
  let targetEntryIdForLoad = $state<string | undefined>(undefined);

  // Initialize with current selection and update when search/sort/filter changes
  $effect.pre(() => {
    // When search/sort/filter changes, use current selection as target for next load
    void search; void sort; void gridifyFilter;
    targetEntryIdForLoad = selectedEntryId;
  });

  // Main resource that loads entries when dependencies change
  const entriesResource = resource(
    () => ({ search, sort, gridifyFilter, miniLcmApi }),
    async (_curr, _prev, { signal }): Promise<IEntry[]> => {
      loadingUndebounced = true;
      try {
        console.log(`[EntriesList] Loading initial window, targetEntryId=${targetEntryIdForLoad}`);
        const window = await fetchEntriesWindow(0, PAGE_SIZE, targetEntryIdForLoad);
        if (signal.aborted) return loadedEntries;
        if (!window) return [];

        console.log(`[EntriesList] Loaded: offset=${window.offset}, entries=${window.entries.length}, total=${window.totalCount}, targetIndex=${window.targetIndex}`);
        windowOffset = window.offset;
        loadedEntries = window.entries;
        totalCount = window.totalCount;

        // Schedule scroll to target after render
        if (window.targetIndex != null && window.targetIndex >= 0) {
          console.log(`[EntriesList] Scheduling scroll to index ${window.targetIndex}`);
          pendingScrollToIndex = window.targetIndex;
        }

        return window.entries;
      } finally {
        loadingUndebounced = false;
      }
    });

  // Apply fw-lite filter if needed
  const entries = $derived.by(() => {
    if (loadedEntries.length && $currentView.type === 'fw-lite') {
      return filterLiteMorphemeTypes(loadedEntries);
    }
    return loadedEntries;
  });

  // Update entry count when loading completes
  watch(() => [entries, loadingUndebounced], () => {
    if (!loadingUndebounced) entryCount = totalCount;
  });

  // Show errors
  $effect(() => {
    if (entriesResource.error) {
      console.error('Failed to load entries:', entriesResource.error);
      AppNotification.error($t`Failed to load entries`, entriesResource.error.message);
    }
  });

  // VList reference and scroll handling
  let vList = $state<VListHandle>();
  let pendingScrollToIndex = $state<number | null>(null);

  // Track when user explicitly selects an entry (vs programmatic/load)
  let userSelectedEntryId = $state<string | undefined>(undefined);

  // Execute pending scroll after render (only for initial load centering)
  $effect(() => {
    if (pendingScrollToIndex != null && vList && !loading.current) {
      vList.scrollToIndex(pendingScrollToIndex, { align: 'center' });
      pendingScrollToIndex = null;
    }
  });

  // Scroll to entry when user explicitly clicks on one (not during loads)
  $effect(() => {
    if (!vList || !userSelectedEntryId || loading.current) return;
    const index = entries.findIndex(e => e.id === userSelectedEntryId);
    if (index === -1) return; // Entry not loaded - don't try to fetch, just ignore

    // Check if entry is already visible
    const scrollOffset = vList.getScrollOffset();
    const viewportSize = vList.getViewportSize();
    const itemOffset = vList.getItemOffset(index);
    const itemSize = vList.getItemSize(index);

    if (itemOffset < scrollOffset || itemOffset + itemSize > scrollOffset + viewportSize) {
      vList.scrollToIndex(index, { align: 'center' });
    }
    // Clear after scrolling so we don't scroll again on next load
    userSelectedEntryId = undefined;
  });

  // Handle scroll to load more entries
  function handleScroll(): void {
    if (!vList || loading.current) return;
    const scrollOffset = vList.getScrollOffset();
    const viewportSize = vList.getViewportSize();
    const startIndex = vList.findItemIndex(scrollOffset);
    const endIndex = vList.findItemIndex(scrollOffset + viewportSize);

    // Load more before if near start
    if (startIndex < LOAD_THRESHOLD && windowOffset > 0) {
      void loadEntriesBefore();
    }

    // Load more after if near end
    const remainingAfter = loadedEntries.length - endIndex;
    if (remainingAfter < LOAD_THRESHOLD && windowOffset + loadedEntries.length < totalCount) {
      void loadEntriesAfter();
    }
  }

  // UI helpers
  const skeletonRowCount = Math.floor(Math.random() * 5) + 3;

  async function handleNewEntry() {
    const entry = await dialogsService.createNewEntry(undefined, {
      semanticDomains: semanticDomain ? [semanticDomain] : [],
      partOfSpeech: partOfSpeech,
    });
    if (entry) {
      userSelectedEntryId = entry.id;
      onSelectEntry(entry);
    }
  }

  export function selectNextEntry() {
    const index = entries.findIndex(e => e.id === selectedEntryId);
    const nextIndex = index === -1 ? 0 : index + 1;

    if (nextIndex < entries.length) {
      userSelectedEntryId = entries[nextIndex].id;
      onSelectEntry(entries[nextIndex]);
      return entries[nextIndex];
    }

    // Need to load more entries
    if (windowOffset + loadedEntries.length < totalCount) {
      void loadEntriesAfter().then(() => {
        if (nextIndex < entries.length) {
          userSelectedEntryId = entries[nextIndex].id;
          onSelectEntry(entries[nextIndex]);
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
        <VList bind:this={vList} data={entries} class="h-full p-0.5 md:pr-3 after:h-12 after:block" getKey={d => d.id} onscroll={handleScroll}>
          {#snippet children(entry)}
            <EntryMenu {entry} contextMenu>
              <EntryRow {entry}
                        class="mb-2"
                        selected={selectedEntryId === entry.id}
                        onclick={() => { userSelectedEntryId = entry.id; onSelectEntry(entry); }}
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
