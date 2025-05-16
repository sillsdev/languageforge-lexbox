<script lang="ts">
  import type { IEntry } from '$lib/dotnet-types';
  import type { IQueryOptions } from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
  import { SortField } from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
  import { Debounced, resource } from 'runed';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import EntryRow from './EntryRow.svelte';
  import Button from '$lib/components/ui/button/button.svelte';
  import { cn } from '$lib/utils';
  import { t } from 'svelte-i18n-lingui';
  import {ScrollArea} from '$lib/components/ui/scroll-area';
  import DevContent from '$lib/layout/DevContent.svelte';
  import NewEntryButton from '../NewEntryButton.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import EntryMenu from './EntryMenu.svelte';
  import FabContainer from '$lib/components/fab/fab-container.svelte';

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
    entriesResource.refetch();
  });
  projectEventBus.onEntryUpdated(entry => {
    if (entriesResource.loading) return;
    entriesResource.refetch();
  });


  const entriesResource = resource(
    () => ({ search, sortDirection, gridifyFilter }),
    async ({ search, sortDirection, gridifyFilter }) => {
      const queryOptions: IQueryOptions = {
        count: 100,
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
    },
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

<ScrollArea class="md:pr-3 flex-1" role="table">
  {#if entriesResource.error}
    <div class="flex items-center justify-center h-full text-muted-foreground">
      <p>{$t`Failed to load entries`}</p>
      <p>{entriesResource.error.message}</p>
    </div>
  {:else}
    <div class="space-y-2 p-0.5 pb-12">
      {#if loading.current}
        <!-- Show skeleton rows while loading -->
        {#each { length: skeletonRowCount }, _index}
          <EntryRow skeleton={true} />
        {/each}
      {:else}
        {#each entries as entry}
          <EntryMenu {entry} contextMenu>
              <EntryRow {entry}
                isSelected={selectedEntryId === entry.id}
                onclick={() => onSelectEntry(entry)}
                {previewDictionary} />
          </EntryMenu>
        {:else}
          <div class="flex items-center justify-center h-full text-muted-foreground">
            <p>{$t`No entries found`}</p>
          </div>
        {/each}
      {/if}
    </div>
  {/if}
</ScrollArea>
