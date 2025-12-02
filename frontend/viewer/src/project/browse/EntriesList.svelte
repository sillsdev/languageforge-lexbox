<script lang="ts">
  import {MorphType, type IEntry, type IPartOfSpeech, type ISemanticDomain} from '$lib/dotnet-types';
  import type {IQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
  import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
  import {Debounced, resource, useDebounce, watch} from 'runed';
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
  import {DEFAULT_DEBOUNCE_TIME} from '$lib/utils/time';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {useCurrentView} from '$lib/views/view-service';

  const LITE_MORPHEME_TYPES = new Set([
    MorphType.Root, MorphType.BoundRoot,
    MorphType.Stem, MorphType.BoundStem,
    MorphType.Particle,
    MorphType.Phrase, MorphType.DiscontiguousPhrase,
  ]);

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

  projectEventBus.onEntryDeleted(entryId => {
    if (selectedEntryId === entryId) onSelectEntry(undefined);
    if (entriesResource.loading || !entries.some(e => e.id === entryId)) return;
    const currentIndex = entriesResource.current?.findIndex(e => e.id === entryId) ?? -1;
    if (currentIndex >= 0) {
      entriesResource.current!.splice(currentIndex, 1);
    }
  });
  projectEventBus.onEntryUpdated(_entry => {
    if (entriesResource.loading) return;
    const currentIndex = entriesResource.current?.findIndex(e => e.id === _entry.id) ?? -1;
    if (currentIndex >= 0) {
      entriesResource.current![currentIndex] = _entry;
    } else {
      void silentlyRefreshEntries();
    }
  });

  async function silentlyRefreshEntries() {
    const updatedEntries = await fetchCurrentEntries(true);
    entriesResource.mutate(updatedEntries);
  }

  let loadingUndebounced = $state(true);
  const loading = new Debounced(() => loadingUndebounced, 50);
  const fetchCurrentEntries = useDebounce(async (silent = false) => {
    if (!miniLcmApi) return [];
    if (!silent) loadingUndebounced = true;
    try {
      const queryOptions: IQueryOptions = {
        count: IsMobile.value ? 1_000 : 5_000,
        offset: 0,
        filter: {
          gridifyFilter: gridifyFilter ? gridifyFilter : undefined,
        },
        order: {
          field: sort?.field ?? SortField.SearchRelevance,
          writingSystem: 'default',
          ascending: sort?.dir !== 'desc',
        },
      };

      const entries = search
        ? miniLcmApi.searchEntries(search, queryOptions)
        : miniLcmApi.getEntries(queryOptions);
      await entries; // ensure the entries have arrived before toggling the loading flag
      return entries;
    } finally {
      loadingUndebounced = false;
    }
  }, DEFAULT_DEBOUNCE_TIME);

  const entriesResource = resource(
    () => ({ search, sort, gridifyFilter, miniLcmApi }),
    async (_curr, _prev, refetchInfo): Promise<IEntry[]> => {
      const entries = await fetchCurrentEntries();
      // don't let slow requests overwrite newer ones
      // if the newer request is finished then entriesResource.current is up-to-date
      // else entriesResource.current is out-of-date, but will be updated by the newer request
      if (refetchInfo.signal.aborted) return entriesResource.current ?? [];
      return entries;
    });
  const entries = $derived.by(() => {
    let currEntries = entriesResource.current ?? [];
    if (currEntries.length && $currentView.type === 'fw-lite') {
      currEntries = filterLiteMorphemeTypes(currEntries);
    }
    return currEntries;
  });
  watch(() => [entries, entriesResource.loading], () => {
    if (!entriesResource.loading)
      entryCount = entries.length;
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
  $effect(() => {
    if (!vList || !selectedEntryId) return;
    const indexOfSelected = entries.findIndex(e => e.id === selectedEntryId);
    if (indexOfSelected === -1) return;
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

  export function selectNextEntry() {
    const indexOfSelected = entries.findIndex(e => e.id === selectedEntryId);
    const nextIndex = indexOfSelected === -1 ? 0 : indexOfSelected + 1;
    let nextEntry = entries[nextIndex];
    onSelectEntry(nextEntry);
    return nextEntry;
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
        <VList bind:this={vList} data={entries ?? []} class="h-full p-0.5 md:pr-3 after:h-12 after:block" getKey={d => d.id} bufferSize={400}>
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
