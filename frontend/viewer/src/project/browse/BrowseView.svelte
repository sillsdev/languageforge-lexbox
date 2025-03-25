<script lang="ts">
  import { ResizableHandle, ResizablePane, ResizablePaneGroup } from '$lib/components/ui/resizable';
  import type { IEntry } from '$lib/dotnet-types';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import EntryView from './EntryView.svelte';
  import SearchFilter from './SearchFilter.svelte';
  import ViewPicker from './ViewPicker.svelte';
  import EntriesList from './EntriesList.svelte';
  import { Badge } from '$lib/components/ui/badge';
  import { Icon } from '$lib/components/ui/icon';
  import { t } from 'svelte-i18n-lingui';
  let selectedEntry = $state<IEntry | undefined>(undefined);
  const defaultLayout = [30, 70]; // Default split: 30% for list, 70% for details
  const isMobile = new IsMobile();
  let search = $state('');
  let gridifyFilter = $state<string | undefined>(undefined);
  let sortDirection = $state<'asc' | 'desc'>('asc');

  function toggleSort() {
    sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
  }

  function resetPanes() {
    if (!leftPane || !rightPane) return;

    const totalSize = leftPane.getSize() + rightPane.getSize();
    leftPane.resize((defaultLayout[0] / 100) * totalSize);
    rightPane.resize((defaultLayout[1] / 100) * totalSize);
  }

  let anyColumnIsCollapsed = $state(false);
  let canClickLeft = $state(true);
  let canClickRight = $state(true);
  let leftPane: ResizablePane | null = $state(null);
  let rightPane: ResizablePane | null = $state(null);
</script>

<div class="flex flex-col h-full">
  <ResizablePaneGroup direction="horizontal" class="flex-1 min-h-0 !overflow-visible" bind:anyColumnIsCollapsed>
    {#if !isMobile.current || !selectedEntry}
      <ResizablePane
        bind:this={leftPane}
        onCollapse={() => (canClickLeft = false)}
        onExpand={() => (canClickLeft = true)}
        defaultSize={defaultLayout[0]}
        collapsible
        collapsedSize={0}
        minSize={15}
        class="min-h-0 flex flex-col relative"
      >
        <div class="p-2">
          <SearchFilter bind:search bind:gridifyFilter />
          <div class="mt-3">
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
    {#if !isMobile.current}
      <ResizableHandle {canClickLeft} {canClickRight} withHandle ondblclick={resetPanes} class={anyColumnIsCollapsed ? 'bg-transparent' : ''}
        onClickLeft={() => { if(canClickRight) leftPane?.collapse(); else rightPane?.expand(); }}
        onClickRight={() => { if(canClickLeft) rightPane?.collapse(); else leftPane?.expand(); }}
        />
    {/if}
    {#if selectedEntry || !isMobile.current}
      <ResizablePane
        bind:this={rightPane}
        onCollapse={() => (canClickRight = false)}
        onExpand={() => (canClickRight = true)}
        defaultSize={defaultLayout[1]} collapsible collapsedSize={0} minSize={15}>
        {#if !selectedEntry}
          <div class="flex items-center justify-center h-full text-muted-foreground">
            <p>Select an entry to view details</p>
          </div>
        {:else}
          <EntryView
            entryId={selectedEntry.id}
            onClose={() => (selectedEntry = undefined)}
            showClose={isMobile.current}
          />
        {/if}
      </ResizablePane>
    {/if}
  </ResizablePaneGroup>
</div>
