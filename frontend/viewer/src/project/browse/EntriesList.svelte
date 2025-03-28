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

  const {
    search = '',
    selectedEntry = undefined,
    sortDirection = 'asc',
    onSelectEntry,
    gridifyFilter = undefined,
  }: {
    search?: string;
    selectedEntry?: IEntry;
    sortDirection: 'asc' | 'desc';
    onSelectEntry: (entry: IEntry) => void;
    gridifyFilter?: string;
  } = $props();
  const miniLcmApi = useMiniLcmApi();

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

  function handleNewEntry() {
    console.log('handleNewEntry');
  }
</script>

<div class="absolute bottom-0 right-0 m-4 flex flex-col items-end z-10">
  <DevContent>
    <Button
      icon="i-mdi-refresh"
      variant="secondary"
      iconProps={{ class: cn(loading.current && 'animate-spin') }}
      size="icon"
      class="mt-4 mb-6"
      onclick={() => entriesResource.refetch()}
    />
  </DevContent>
  <NewEntryButton onclick={handleNewEntry} shortForm />
</div>

<ScrollArea class="md:pr-5 flex-1" role="table">
  {#if entriesResource.error}
    <div class="flex items-center justify-center h-full text-muted-foreground">
      <p>{$t`Failed to load entries`}</p>
      <p>{entriesResource.error.message}</p>
    </div>
  {:else}
    <div class="space-y-2 pb-12">
      {#if loading.current}
        <!-- Show skeleton rows while loading -->
        {#each { length: skeletonRowCount }, _index}
          <EntryRow skeleton={true} />
        {/each}
      {:else}
        {#each entries as entry}
          <EntryRow {entry} isSelected={selectedEntry === entry} onclick={() => onSelectEntry(entry)} />
        {:else}
          <div class="flex items-center justify-center h-full text-muted-foreground">
            <p>{$t`No entries found`}</p>
          </div>
        {/each}
      {/if}
    </div>
  {/if}
</ScrollArea>
