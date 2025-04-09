<script lang="ts">
  import { ResizableHandle, ResizablePane, ResizablePaneGroup } from '$lib/components/ui/resizable';
  import type { IEntry } from '$lib/dotnet-types';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import EntryView from './EntryView.svelte';
  import SearchFilter from './SearchFilter.svelte';
  import EntriesList from './EntriesList.svelte';
  import { Badge } from '$lib/components/ui/badge';
  import { Icon } from '$lib/components/ui/icon';
  import { t } from 'svelte-i18n-lingui';
  let selectedEntry = $state<IEntry | undefined>(undefined);
  const defaultLayout = [30, 70] as const; // Default split: 30% for list, 70% for details
  let search = $state('');
  let gridifyFilter = $state<string | undefined>(undefined);
  let sortDirection = $state<'asc' | 'desc'>('asc');

  function toggleSort() {
    sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
  }

  let leftPane: ResizablePane | undefined = $state();
  let rightPane: ResizablePane | undefined = $state();
</script>

<div class="flex flex-col h-full p-2 md:p-4">
  <ResizablePaneGroup direction="horizontal" class="flex-1 min-h-0 !overflow-visible">
    {#if !IsMobile.value || !selectedEntry}
      <ResizablePane
        bind:this={leftPane}
        defaultSize={defaultLayout[0]}
        collapsible
        collapsedSize={0}
        minSize={15}
        class="min-h-0 flex flex-col relative"
      >
        <div class="mb-2 md:mb-4 md:mr-4">
          <SearchFilter bind:search bind:gridifyFilter />
          <div class="mt-2 md:mt-3">
            <Badge
              variant="secondary"
              class="cursor-pointer"
              onclick={toggleSort}
            >
            <Icon icon={sortDirection === 'asc' ? 'i-mdi-sort-alphabetical-ascending' : 'i-mdi-sort-alphabetical-descending'} class="h-4 w-4" />
              {$t`Headword`}
            </Badge>
          </div>
        </div>
        <EntriesList {search} {selectedEntry} {sortDirection} {gridifyFilter} onSelectEntry={(e) => (selectedEntry = e)} />
      </ResizablePane>
    {/if}
    {#if !IsMobile.value}
      <ResizableHandle {leftPane} {rightPane} withHandle resetTo={defaultLayout} />
    {/if}
    {#if selectedEntry || !IsMobile.value}
      <ResizablePane
        bind:this={rightPane}
        defaultSize={defaultLayout[1]} collapsible collapsedSize={0} minSize={15}>
        {#if !selectedEntry}
          <div class="flex items-center justify-center h-full text-muted-foreground">
            <p>Select an entry to view details</p>
          </div>
        {:else}
          <EntryView
            entryId={selectedEntry.id}
            onClose={() => (selectedEntry = undefined)}
            showClose={IsMobile.value}
          />
        {/if}
      </ResizablePane>
    {/if}
  </ResizablePaneGroup>
</div>
