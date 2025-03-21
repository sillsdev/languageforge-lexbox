<script lang="ts">
  import type { IEntry } from '$lib/dotnet-types';
  import type { IQueryOptions } from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
  import { SortField } from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
  import { resource } from 'runed';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import EntryRow from './EntryRow.svelte';
  import Button from '$lib/components/ui/button/button.svelte';
  import { cn } from '$lib/utils';

  const {
    search = '',
    selectedEntry = undefined,
    sortDirection = 'asc',
    onSelectEntry,
  }: {
    search?: string;
    selectedEntry?: IEntry;
    sortDirection: 'asc' | 'desc';
    onSelectEntry: (entry: IEntry) => void;
  } = $props();

  const miniLcmApi = useMiniLcmApi();

  const entriesResource = resource(
    () => ({ search, sortDirection }),
    async ({ search, sortDirection }) => {
      await new Promise((resolve) => setTimeout(resolve, 1000));
      const queryOptions: IQueryOptions = {
        count: 100,
        offset: 0,
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

  // Generate a random number of skeleton rows between 3 and 7
  const skeletonRowCount = Math.floor(Math.random() * 5) + 3;
</script>

<Button
  icon="i-mdi-refresh"
  iconProps={{ class: cn(entriesResource.loading && 'animate-spin') }}
  size="icon"
  class="absolute bottom-0 right-0 m-4"
  onclick={() => entriesResource.refetch()}
/>
<div class="overflow-y-auto flex-1 pr-4">
  <div class="space-y-2">
    {#if entriesResource.loading}
      <!-- Show skeleton rows while loading -->
      {#each { length: skeletonRowCount }, _index}
        <EntryRow skeleton={true} />
      {/each}
    {:else if entriesResource.error}
      <div class="flex items-center justify-center h-full text-muted-foreground">
        <p>Failed to load entries</p>
        <p>{entriesResource.error.message}</p>
      </div>
    {:else}
      {#each entries as entry}
        <EntryRow {entry} isSelected={selectedEntry === entry} onclick={() => onSelectEntry(entry)} />
      {:else}
        <div class="flex items-center justify-center h-full text-muted-foreground">
          <p>No entries found</p>
        </div>
      {/each}
    {/if}
  </div>
</div>
