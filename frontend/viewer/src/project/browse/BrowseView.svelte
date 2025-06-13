<script lang="ts">
  import { ResizableHandle, ResizablePane, ResizablePaneGroup } from '$lib/components/ui/resizable';
  import { IsMobile } from '$lib/hooks/is-mobile.svelte';
  import EntryView from './EntryView.svelte';
  import SearchFilter from './SearchFilter.svelte';
  import EntriesList from './EntriesList.svelte';
  import { badgeVariants } from '$lib/components/ui/badge';
  import { Icon } from '$lib/components/ui/icon';
  import { t } from 'svelte-i18n-lingui';
  import SidebarPrimaryAction from '../SidebarPrimaryAction.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import NewEntryButton from '../NewEntryButton.svelte';
  import ResponsivePopup from '$lib/components/responsive-popup/responsive-popup.svelte';
  import {Tabs, TabsList, TabsTrigger} from '$lib/components/ui/tabs';
  import {Button} from '$lib/components/ui/button';
  import {QueryParamState} from '$lib/utils/url.svelte';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import * as ResponsiveMenu from '$lib/components/responsive-menu';
  import {SortField} from '$lib/dotnet-types';
  import {cn} from '$lib/utils';

  const currentView = useCurrentView();
  const dialogsService = useDialogsService();
  const selectedEntryId = new QueryParamState({key: 'entryId', allowBack: true, replaceOnDefaultValue: true});
  const defaultLayout = [30, 70] as const; // Default split: 30% for list, 70% for details
  let search = $state('');
  let gridifyFilter = $state<string | undefined>(undefined);
  let sortField = $state<SortField>(SortField.SearchRelevance);
  let sortDirection = $state<'asc' | 'desc'>('asc');
  let entryMode: 'preview' | 'simple' = $state('simple');

  const sortOptions = [
    { value: SortField.SearchRelevance, dir: 'asc', label: $t`Best match`, icon: 'i-mdi-arrow-down' },
    { value: SortField.Headword, dir: 'asc', label: $t`Headword`, icon: 'i-mdi-sort-alphabetical-ascending' },
    { value: SortField.Headword, dir: 'desc', label: $t`Headword`, icon: 'i-mdi-sort-alphabetical-descending' }
  ] as const;

  async function newEntry() {
    const entry = await dialogsService.createNewEntry();
    if (!entry) return;
    selectedEntryId.current = entry.id;
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
    <IfOnce show={!IsMobile.value || !selectedEntryId.current}>
      <ResizablePane
        bind:this={leftPane}
        defaultSize={defaultLayout[0]}
        collapsible
        collapsedSize={0}
        minSize={15}
        class="min-h-0 flex flex-col relative"
      >
        <div class="flex flex-col h-full p-2 md:p-4 md:pr-0">
          <div class="md:mr-3">
            <SearchFilter bind:search bind:gridifyFilter />
            <div class="my-2 flex items-center justify-between">
              <ResponsiveMenu.Root>
                <ResponsiveMenu.Trigger class={badgeVariants({ variant: 'secondary' })}>
                  {#snippet child({props})}
                    <button {...props}>
                      {#if sortField === SortField.Headword}
                        <Icon icon={sortDirection === 'asc' ? 'i-mdi-sort-alphabetical-ascending' : 'i-mdi-sort-alphabetical-descending'} class="h-4 w-4" />
                        {$t`Headword`}
                      {:else}
                        <Icon icon="i-mdi-arrow-down" class="h-4 w-4" />
                        {$t`Best match`}
                      {/if}
                    </button>
                  {/snippet}
                </ResponsiveMenu.Trigger>
                <ResponsiveMenu.Content>
                  {#each sortOptions as option}
                    <ResponsiveMenu.Item
                      onSelect={() => {
                        sortField = option.value;
                        sortDirection = option.dir;
                      }}
                      class={cn(sortField === option.value && sortDirection === option.dir && 'bg-muted')}
                      >
                      <Icon icon={option.icon} />
                      {option.label}
                    </ResponsiveMenu.Item>
                  {/each}
                </ResponsiveMenu.Content>
              </ResponsiveMenu.Root>
              <ResponsivePopup title={$t`List mode`}>
                {#snippet trigger({props})}
                  <Button {...props} size="xs-icon" variant="ghost" icon="i-mdi-format-list-text" />
                {/snippet}
                <div class="space-y-6">
                  <Tabs bind:value={entryMode} class="mb-1">
                    <TabsList>
                      <TabsTrigger value="simple">
                        <Icon icon="i-mdi-format-list-bulleted-square" class="mr-1"/>
                        {$t`Simple`}
                      </TabsTrigger>
                      <TabsTrigger value="preview">
                        <Icon icon="i-mdi-format-list-text" class="mr-1"/>
                        {$t`Preview`}
                      </TabsTrigger>
                    </TabsList>
                  </Tabs>
                </div>
              </ResponsivePopup>
            </div>
          </div>
          <EntriesList {search}
                       selectedEntryId={selectedEntryId.current}
                       {sortField}
                       {sortDirection}
                       {gridifyFilter}
                       onSelectEntry={(e) => (selectedEntryId.current = e?.id ?? '')}
                       previewDictionary={entryMode === 'preview'}/>
        </div>
      </ResizablePane>
    </IfOnce>
    {#if !IsMobile.value}
      <ResizableHandle class="my-4" {leftPane} {rightPane} withHandle resetTo={defaultLayout} />
    {/if}
    {#if selectedEntryId.current || !IsMobile.value}
      <ResizablePane
        bind:this={rightPane}
        defaultSize={defaultLayout[1]} collapsible collapsedSize={0} minSize={15}>
          {#if !selectedEntryId.current}
            <div class="flex items-center justify-center h-full text-muted-foreground text-center m-2">
              <p>{$t`Select ${pt($t`an entry`, $t`a word`, $currentView)} to view details`}</p>
            </div>
          {:else}
            <div class="md:p-4 md:pl-4 h-full">
              <EntryView
                entryId={selectedEntryId.current}
                onClose={() => (selectedEntryId.current = '')}
                showClose={IsMobile.value}
              />
            </div>
          {/if}
      </ResizablePane>
    {/if}
  </ResizablePaneGroup>
</div>
