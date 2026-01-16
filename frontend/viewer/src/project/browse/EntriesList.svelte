<script lang="ts">
  import {type IEntry, type IPartOfSpeech, type ISemanticDomain} from '$lib/dotnet-types';
  import {Debounced} from 'runed';
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
  import type {SortConfig} from './sort/options';
  import {AppNotification} from '$lib/notifications/notifications';
  import {Icon} from '$lib/components/ui/icon';
  import {useProjectContext} from '$project/project-context.svelte';
  import Delayed from '$lib/components/Delayed.svelte';
  import {EntryLoaderService} from '$lib/services/entry-loader-service.svelte';

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

  // Create the entry loader service
  const entryLoader = new EntryLoaderService({
    miniLcmApi: () => miniLcmApi,
    search: () => search,
    sort: () => sort,
    gridifyFilter: () => gridifyFilter,
  });

  // Debounce the loading state for smoother UI
  const loading = new Debounced(() => entryLoader.loading, 50);

  // Keep entryCount in sync
  $effect(() => {
    entryCount = entryLoader.totalCount ?? null;
  });

  // Handle entry deleted events
  projectEventBus.onEntryDeleted(entryId => {
    if (selectedEntryId === entryId) onSelectEntry(undefined);
    entryLoader.removeEntryById(entryId);
  });

  // Handle entry updated events
  projectEventBus.onEntryUpdated(entry => {
    entryLoader.updateEntry(entry);
  });

  $effect(() => {
    if (entryLoader.error) {
      AppNotification.error($t`Failed to load entries`, entryLoader.error.message);
    }
  });

  // Generate a random number of skeleton rows between 3 and 7
  const skeletonRowCount = Math.floor(Math.random() * 5) + 3;

  // Generate index array for virtual list
  const indexArray = $derived(
    entryLoader.totalCount !== undefined
      ? Array.from({ length: entryLoader.totalCount }, (_, i) => i)
      : []
  );

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
    const indexOfSelected = entryLoader.getIndexById(selectedEntryId);
    if (indexOfSelected === undefined) return;
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

  export async function selectNextEntry(): Promise<IEntry | undefined> {
    const indexOfSelected = selectedEntryId
      ? entryLoader.getIndexById(selectedEntryId)
      : undefined;
    const nextIndex = indexOfSelected === undefined ? 0 : indexOfSelected + 1;

    // Check count bounds
    if (entryLoader.totalCount === undefined || nextIndex >= entryLoader.totalCount) {
      return undefined;
    }

    const nextEntry = await entryLoader.loadEntryByIndex(nextIndex);
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
      onclick={() => {
        entryLoader.reset();
        void entryLoader.loadCount();
      }}
    />
  </DevContent>
  {#if !disableNewEntry}
    <PrimaryNewEntryButton onclick={handleNewEntry} shortForm />
  {/if}
</FabContainer>

<div class="flex-1 h-full" role="table">
  {#if entryLoader.error}
    <div class="flex items-center justify-center h-full text-muted-foreground gap-2">
      <Icon icon="i-mdi-alert-circle-outline" />
      <p>{$t`Failed to load entries`}</p>
    </div>
  {:else}
    <div class="h-full">
      {#if loading.current && indexArray.length === 0}
        <div class="md:pr-3 p-0.5">
          <!-- Show skeleton rows while loading initial count -->
          {#each { length: skeletonRowCount }, _index}
            <EntryRow class="mb-2" skeleton={true} />
          {/each}
        </div>
      {:else}
        <VList bind:this={vList} data={indexArray} class="h-full p-0.5 md:pr-3 after:h-12 after:block" getKey={(index: number) => index} bufferSize={400}>
          {#snippet children(index: number)}
            <Delayed
              getCached={() => entryLoader.getEntryByIndex(index)}
              load={() => entryLoader.loadEntryByIndex(index)}
              delay={250}
            >
              {#snippet children(state)}
                {#if state.loading || !state.current}
                  <EntryRow class="mb-2" skeleton={true} />
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
          {/snippet}
        </VList>
        {#if indexArray.length === 0}
          <div class="flex items-center justify-center h-full text-muted-foreground">
            <p>{$t`No entries found`}</p>
          </div>
        {/if}
      {/if}
    </div>
  {/if}
</div>
