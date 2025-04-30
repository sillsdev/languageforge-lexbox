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
  import SidebarPrimaryAction from '../SidebarPrimaryAction.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import NewEntryButton from '../NewEntryButton.svelte';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Tabs, TabsList, TabsTrigger} from '$lib/components/ui/tabs';
  import {Button} from '$lib/components/ui/button';

  const dialogsService = useDialogsService();
  let selectedEntry = $state<IEntry | undefined>(undefined);
  const defaultLayout = [30, 70] as const; // Default split: 30% for list, 70% for details
  let search = $state('');
  let gridifyFilter = $state<string | undefined>(undefined);
  let sortDirection = $state<'asc' | 'desc'>('asc');
  let entryMode: 'preview' | 'simple' = $state('simple');

  function toggleSort() {
    sortDirection = sortDirection === 'asc' ? 'desc' : 'asc';
  }

  async function newEntry() {
    const entry = await dialogsService.createNewEntry();
    if (!entry) return;
    selectedEntry = entry;
  }

  let leftPane: ResizablePane | undefined = $state();
  let rightPane: ResizablePane | undefined = $state();
</script>
<SidebarPrimaryAction>
  {#snippet children(isOpen: boolean)}
    <NewEntryButton active={!IsMobile.value && isOpen} onclick={newEntry}/>
  {/snippet}
</SidebarPrimaryAction>
<div class="flex flex-col h-full">
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
        <div class="flex flex-col h-full p-2 md:p-4 md:pr-1">
          <div class="md:mr-3">
            <SearchFilter bind:search bind:gridifyFilter />
            <div class="my-2 flex items-center justify-between">
              <Badge
                variant="secondary"
                class="cursor-pointer"
                onclick={toggleSort}
              >
                <Icon icon={sortDirection === 'asc' ? 'i-mdi-sort-alphabetical-ascending' : 'i-mdi-sort-alphabetical-descending'} class="h-4 w-4" />
                {$t`Headword`}
              </Badge>
              <ResponsivePopup title={$t`List View`}>
                {#snippet trigger({props})}
                  <Button {...props} size="xs-icon" variant="ghost" icon="i-mdi-format-list-text" />
                {/snippet}
                <div class="space-y-6">
                  <Tabs bind:value={entryMode} class="mb-1">
                    <h3 class="font-normal mb-1">{$t`Row display`}</h3>
                    <TabsList>
                      <TabsTrigger value="simple">
                        <Icon icon="i-mdi-format-list-bulleted-square" class="mr-1"/>
                        Simple
                      </TabsTrigger>
                      <TabsTrigger value="preview">
                        <Icon icon="i-mdi-format-list-text" class="mr-1"/>
                        Preview
                      </TabsTrigger>
                    </TabsList>
                  </Tabs>
                </div>
              </ResponsivePopup>
            </div>
          </div>
          <EntriesList {search}
                       {selectedEntry}
                       {sortDirection}
                       {gridifyFilter}
                       onSelectEntry={(e) => (selectedEntry = e)}
                       previewDictionary={entryMode === 'preview'}/>
        </div>
      </ResizablePane>
    {/if}
    {#if !IsMobile.value}
      <ResizableHandle class="my-4" {leftPane} {rightPane} withHandle resetTo={defaultLayout} />
    {/if}
    {#if selectedEntry || !IsMobile.value}
      <ResizablePane
        bind:this={rightPane}
        defaultSize={defaultLayout[1]} collapsible collapsedSize={0} minSize={15}>
          {#if !selectedEntry}
            <div class="flex items-center justify-center h-full text-muted-foreground text-center m-2">
              <p>Select an entry to view details</p>
            </div>
          {:else}
            <div class="p-2 md:p-4 md:pl-6 h-full">
              <EntryView
                entryId={selectedEntry.id}
                onClose={() => (selectedEntry = undefined)}
                showClose={IsMobile.value}
              />
            </div>
          {/if}
      </ResizablePane>
    {/if}
  </ResizablePaneGroup>
</div>
