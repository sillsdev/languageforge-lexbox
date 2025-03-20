<script lang="ts">
  import { ResizableHandle, ResizablePane, ResizablePaneGroup } from '$lib/components/ui/resizable';
  import type { IEntry } from '$lib/dotnet-types';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import EntryView from './EntryView.svelte';
  import SearchFilter from './SearchFilter.svelte';
  import ViewPicker from './ViewPicker.svelte';
  import EntriesList from './EntriesList.svelte';

  let selectedEntry = $state<IEntry | undefined>(undefined);
  const defaultLayout = [30, 70]; // Default split: 30% for list, 70% for details
  const isMobile = new IsMobile();
  let search = $state('');
</script>

<div class="flex flex-col h-full p-4">

  <ResizablePaneGroup direction="horizontal" class="flex-1 min-h-0 overflow-hidden">
    {#if !isMobile.current || !selectedEntry}
      <ResizablePane
        defaultSize={defaultLayout[0]}
        collapsible
        collapsedSize={0}
        minSize={15}
        class="min-h-0 flex flex-col relative"
      >
        <div class="p-2 pr-4">
            <SearchFilter bind:search />
            <ViewPicker />
        </div>
        <EntriesList
          {search}
          {selectedEntry}
          onSelectEntry={e => selectedEntry = e}
        />
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
