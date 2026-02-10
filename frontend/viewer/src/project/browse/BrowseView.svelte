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
  import {QueryParamState} from '$lib/utils/url.svelte';
  import {pt} from '$lib/views/view-text';
  import {useCurrentView} from '$lib/views/view-service';
  import IfOnce from '$lib/components/if-once/if-once.svelte';
  import {SortField, type IPartOfSpeech, type IPublication, type ISemanticDomain} from '$lib/dotnet-types';
  import SortMenu from './sort/SortMenu.svelte';
  import type {SortConfig} from './sort/options';
  import {useProjectContext} from '$project/project-context.svelte';
  import type {EntryListViewMode} from './EntryListViewOptions.svelte';
  import EntryListViewOptions from './EntryListViewOptions.svelte';

  const projectContext = useProjectContext();
  const currentView = useCurrentView();
  const dialogsService = useDialogsService();
  const selectedEntryId = new QueryParamState({key: 'entryId', allowBack: true, replaceOnDefaultValue: true});
  const defaultLayout = [30, 70] as const; // Default split: 30% for list, 70% for details
  let search = $state('');
  let gridifyFilter = $state<string>();
  let publication = $state<IPublication>();
  let semanticDomain = $state<ISemanticDomain>();
  let partOfSpeech = $state<IPartOfSpeech>();
  let sort = $state<SortConfig>();
  let entryMode: EntryListViewMode = $state('simple');

  async function newEntry() {
    const entry = await dialogsService.createNewEntry(undefined, {
      publishIn: publication ? [publication] : [],
    }, {
      semanticDomains: semanticDomain ? [semanticDomain] : undefined,
      partOfSpeech,
    });
    if (!entry) return;
    selectedEntryId.current = entry.id;
  }

  let leftPane: ResizablePane | undefined = $state();
  let rightPane: ResizablePane | undefined = $state();
</script>
<SidebarPrimaryAction>
  {#snippet children(isOpen: boolean)}
    <PrimaryNewEntryButton active={!IsMobile.value && isOpen} onclick={newEntry}/>
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
        class="min-h-0 flex flex-col relative @container/list"
      >
        <div class="flex flex-col h-full p-2 md:p-4 md:pr-0">
          <div class="md:mr-3">
            <SearchFilter bind:search bind:gridifyFilter bind:publication bind:semanticDomain bind:partOfSpeech />
            <div class="my-2 flex items-center justify-between">
              <SortMenu bind:value={sort}
                autoSelector={() => search ? SortField.SearchRelevance : SortField.Headword} />
              <EntryListViewOptions bind:entryMode />
            </div>
          </div>
          <EntriesList {search}
                       selectedEntryId={selectedEntryId.current}
                       {sort}
                       {gridifyFilter}
                       {publication}
                       {partOfSpeech}
                       {semanticDomain}
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
          {:else if projectContext.maybeApi}
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
