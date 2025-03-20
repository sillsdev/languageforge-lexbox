<script lang="ts">
  import { Icon } from '$lib/components/ui/icon';
  import { ResizableHandle, ResizablePane, ResizablePaneGroup } from '$lib/components/ui/resizable';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import type { IEntry } from '$lib/dotnet-types';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import { t } from 'svelte-i18n-lingui';
  import EntryRow from './EntryRow.svelte';
  import EntryView from './EntryView.svelte';
  import SearchFilter from './SearchFilter.svelte';
  import ViewPicker from './ViewPicker.svelte';
  import { resource } from 'runed';

  const miniLcmApi = useMiniLcmApi();

  let selectedEntry = $state<IEntry | undefined>(undefined);
  const defaultLayout = [30, 70]; // Default split: 30% for list, 70% for details
  const isMobile = new IsMobile();
  let search = $state('');

  const entriesResource = resource(
    () => search,
    async (search) => {
      if (search) {
        return miniLcmApi.searchEntries(search);
      }
      return miniLcmApi.getEntries(undefined);
    },
  );
  const entries = $derived(entriesResource.current ?? []);
</script>

<div class="flex flex-col h-full p-4">

  <ResizablePaneGroup direction="horizontal" class="flex-1 min-h-0 overflow-hidden">
    {#if !isMobile.current || !selectedEntry}
      <ResizablePane
        defaultSize={defaultLayout[0]}
        collapsible
        collapsedSize={0}
        minSize={15}
        class="min-h-0 flex flex-col"
      >
        <div class="p-2 pr-4">
            <SearchFilter bind:search />
            <ViewPicker />
        </div>
        <div class="overflow-y-auto flex-1 pr-4">
          <div class="space-y-2">
            {#if entriesResource.loading}
              <div class="flex items-center justify-center h-full text-muted-foreground">
                <p>Loading entries...</p>
              </div>
            {:else if entriesResource.error}
              <div class="flex items-center justify-center h-full text-muted-foreground">
                <p>Failed to load entries</p>
                <p>{entriesResource.error.message}</p>
              </div>
            {:else}
              {#each entries as entry}
                <EntryRow {entry} isSelected={selectedEntry === entry} onclick={() => (selectedEntry = entry)} />
              {:else}
                <div class="flex items-center justify-center h-full text-muted-foreground">
                  <p>No entries found</p>
                </div>
              {/each}
            {/if}
          </div>
        </div>
      </ResizablePane>
    {/if}
    {#if !isMobile.current}
      <ResizableHandle withHandle />
    {/if}
    {#if !isMobile.current || selectedEntry}
      <ResizablePane defaultSize={defaultLayout[1]} collapsible collapsedSize={0} minSize={15}>
        <EntryView
          entry={selectedEntry}
          onClose={() => (selectedEntry = undefined)}
          showClose={isMobile.current}
        />
      </ResizablePane>
    {/if}
  </ResizablePaneGroup>
</div>
