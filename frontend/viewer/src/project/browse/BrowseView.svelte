<script lang="ts">
  import {ResizableHandle, ResizablePane, ResizablePaneGroup} from '$lib/components/ui/resizable';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import EntryView from './EntryView.svelte';
  import SearchFilter from './SearchFilter.svelte';
  import EntriesList from './EntriesList.svelte';
  import {t} from 'svelte-i18n-lingui';
  import SidebarPrimaryAction from '../SidebarPrimaryAction.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import PrimaryNewEntryButton from '../PrimaryNewEntryButton.svelte';
  import {QueryParamState, QueryParamStateBool} from '$lib/utils/url.svelte';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import {SortField, type IPartOfSpeech, type ISemanticDomain} from '$lib/dotnet-types';
  import SortMenu from './sort/SortMenu.svelte';
  import type {SortConfig} from './sort/options';
  import {useProjectContext} from '$project/project-context.svelte';
  import type {EntryListViewMode} from './EntryListViewOptions.svelte';
  import EntryListViewOptions from './EntryListViewOptions.svelte';

  const projectContext = useProjectContext();
  const currentView = useCurrentView();
  const dialogsService = useDialogsService();

  // DESKTOP: the entry is a sibling of the list (it's a split view). We can switch between selected entries.
  // So, selectedEntryId itself drives navigation.
  // MOBILE: an entry is a child of the list (it's hierarchical). We always go back to the list before opening a different entry.
  // So, entryOpen drives navigation.
  const selectedEntryId = new QueryParamState({key: 'entryId', allowBack: !IsMobile.value, replaceOnDefaultValue: !IsMobile.value});
  const entryOpen = new QueryParamStateBool({key: 'entryOpen', allowBack: IsMobile.value, replaceOnDefaultValue: IsMobile.value}, false);
  const defaultLayout = [30, 70] as const; // Default split: 30% for list, 70% for details
  let search = $state('');
  let gridifyFilter = $state<string>();
  let semanticDomain = $state<ISemanticDomain>();
  let partOfSpeech = $state<IPartOfSpeech>();
  let sort = $state<SortConfig>();
  let entryMode: EntryListViewMode = $state('simple');

  async function newEntry() {
    const entry = await dialogsService.createNewEntry(undefined, {
      semanticDomains: semanticDomain ? [semanticDomain] : undefined,
      partOfSpeech,
    });
    if (!entry) return;
    selectedEntryId.current = entry.id;
  }

  let leftPane: ResizablePane | undefined = $state();
  let rightPane: ResizablePane | undefined = $state();
  let entriesList: EntriesList | undefined = $state();

  async function onCloseEntry() {
    if (IsMobile.value) {
      // we preserve the selected entry, so we can scroll to it on filter changes (like desktop)
      entryOpen.current = false;
      // it can creep out of view on mobile
      await entriesList?.tryToScrollToEntry(selectedEntryId.current);
    }
  }
</script>
<SidebarPrimaryAction>
  {#snippet children(isOpen: boolean)}
    <PrimaryNewEntryButton active={!IsMobile.value && isOpen} onclick={newEntry}/>
  {/snippet}
</SidebarPrimaryAction>
<div class="flex flex-col h-full">
  <ResizablePaneGroup direction="horizontal" class="flex-1 min-h-0 !overflow-visible">
    <IfOnce show={!IsMobile.value || !selectedEntryId.current || !entryOpen.current}>
      <ResizablePane
        bind:this={leftPane}
        defaultSize={defaultLayout[0]}
        collapsible
        collapsedSize={0}
        minSize={15}
        class="min-h-0 flex flex-col relative @container/list"
      >
        <div class="flex flex-col h-full p-2 md:p-4 md:pr-0">
          <div class="md:mr-3">
            <SearchFilter bind:search bind:gridifyFilter bind:semanticDomain bind:partOfSpeech />
            <div class="my-2 flex items-center justify-between">
              <SortMenu bind:value={sort}
                autoSelector={() => search ? SortField.SearchRelevance : SortField.Headword} />
              <EntryListViewOptions bind:entryMode />
            </div>
          </div>
          <EntriesList bind:this={entriesList}
                       {search}
                       selectedEntryId={selectedEntryId.current}
                       {sort}
                       {gridifyFilter}
                       {partOfSpeech}
                       {semanticDomain}
                       onSelectEntry={(e) => {
                        selectedEntryId.current = e?.id ?? '';
                        entryOpen.current = !!selectedEntryId.current;
                       }}
                       previewDictionary={entryMode === 'preview'}/>
        </div>
      </ResizablePane>
    </IfOnce>
    {#if !IsMobile.value}
      <ResizableHandle class="my-4" {leftPane} {rightPane} withHandle resetTo={defaultLayout} />
    {/if}
    {#if !IsMobile.value || (selectedEntryId.current && entryOpen.current)}
      <ResizablePane
        bind:this={rightPane}
        defaultSize={defaultLayout[1]} collapsible collapsedSize={0} minSize={15}>
          {#if !selectedEntryId.current}
            <div class="flex items-center justify-center h-full text-muted-foreground text-center m-2">
              <p>{$t`Select ${pt($t`an entry`, $t`a word`, $currentView)} to view details`}</p>
            </div>
          {:else if projectContext.maybeApi}
            <div class="md:p-4 md:pl-4 h-full">
              <EntryView
                entryId={selectedEntryId.current}
                onClose={onCloseEntry}
                showClose={IsMobile.value}
              />
            </div>
          {/if}
      </ResizablePane>
    {/if}
  </ResizablePaneGroup>
</div>
