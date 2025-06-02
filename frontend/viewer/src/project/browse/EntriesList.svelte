<script lang="ts">
  import type {IEntry} from '$lib/dotnet-types';
  import type {IQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
  import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
  import {Debounced, resource} from 'runed';
  import {useMiniLcmApi} from '$lib/services/service-provider';
  import EntryRow from './EntryRow.svelte';
  import Button from '$lib/components/ui/button/button.svelte';
  import {cn} from '$lib/utils';
  import {t} from 'svelte-i18n-lingui';
  import DevContent from '$lib/layout/DevContent.svelte';
  import NewEntryButton from '../NewEntryButton.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import EntryMenu from './EntryMenu.svelte';
  import FabContainer from '$lib/components/fab/fab-container.svelte';
  import {VList, type VListHandle} from 'virtua/svelte';

  const {
    search = '',
    selectedEntryId = undefined,
    sortDirection = 'asc',
    onSelectEntry,
    gridifyFilter = undefined,
    previewDictionary = false
  }: {
    search?: string;
    selectedEntryId?: string;
    sortDirection: 'asc' | 'desc';
    onSelectEntry: (entry?: IEntry) => void;
    gridifyFilter?: string;
    previewDictionary?: boolean
  } = $props();
  const miniLcmApi = useMiniLcmApi();
  const dialogsService = useDialogsService();
  const projectEventBus = useProjectEventBus();

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
    const updatedEntries = await fetchCurrentEntries();
    entriesResource.mutate(updatedEntries);
  }

  function fetchCurrentEntries(): Promise<IEntry[]> {
    const queryOptions: IQueryOptions = {
      count: 10_000,
      offset: 0,
      filter: {
        gridifyFilter: gridifyFilter ? gridifyFilter : undefined,
      },
      order: {
        field: SortField.Headword,
        writingSystem: 'default',
        ascending: sortDirection === 'asc',
      },
    };

    if (search) {
      return miniLcmApi.searchEntries(search, queryOptions);
    }
    return miniLcmApi.getEntries(queryOptions);
  }

  const entriesResource = resource(
    () => ({ search, sortDirection, gridifyFilter }),
    () => fetchCurrentEntries(),
    {
      debounce: 300,
    }
  );
  const entries = $derived(entriesResource.current ?? []);
  const loading = new Debounced(() => entriesResource.loading, 50);

  // Generate a random number of skeleton rows between 3 and 7
  const skeletonRowCount = Math.floor(Math.random() * 5) + 3;

  async function handleNewEntry() {
    const entry = await dialogsService.createNewEntry();
    if (!entry) return;
    onSelectEntry(entry);
  }

  let vList = $state<VListHandle>();
  $effect(() => {
    if (!vList || !selectedEntryId) return;
    const indexOfSelected = entries.findIndex(e => e.id === selectedEntryId);
    if (indexOfSelected === -1) return;
    if (indexOfSelected > vList.findEndIndex() || indexOfSelected < vList.findStartIndex())
    {
      //using smooth scroll caused lag, maybe only do it if scrolling a short distance?
      vList.scrollToIndex(indexOfSelected, {align: 'center'});
    }
  });

</script>

<FabContainer>
  <DevContent>
    <Button
      icon={loading.current ? 'i-mdi-loading' : 'i-mdi-refresh'}
      variant="outline"
      iconProps={{ class: cn(loading.current && 'animate-spin') }}
      size="icon"
      class="mb-4"
      onclick={() => entriesResource.refetch()}
    />
  </DevContent>
  <NewEntryButton onclick={handleNewEntry} shortForm />
</FabContainer>

<div class="flex-1 h-full" role="table">
  {#if entriesResource.error}
    <div class="flex items-center justify-center h-full text-muted-foreground">
      <p>{$t`Failed to load entries`}</p>
      <p>{entriesResource.error.message}</p>
    </div>
  {:else}
    <div class="h-full">
      {#if loading.current}
        <div class="md:pr-3">
          <!-- Show skeleton rows while loading -->
          {#each { length: skeletonRowCount }, _index}
            <EntryRow class="my-2" skeleton={true} />
          {/each}
        </div>
      {:else}
        <VList bind:this={vList} data={entries ?? []} class="h-full p-0.5 md:pr-3 pb-12" getKey={d => d.id}>
          {#snippet children(entry)}
            <EntryMenu {entry} contextMenu>
              <EntryRow {entry}
                        class="my-1"
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
