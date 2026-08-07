<script lang="ts">
  import Hotkey from '$lib/components/hotkey/hotkey.svelte';
  import MasterDetailView from '$lib/components/master-detail/MasterDetailView.svelte';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import EntryView from './EntryView.svelte';
  import SearchFilter from './SearchFilter.svelte';
  import EntriesList from './EntriesList.svelte';
  import {t} from 'svelte-i18n-lingui';
  import SidebarPrimaryAction from '../SidebarPrimaryAction.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {useFeatures} from '$lib/services/feature-service';
  import PrimaryNewEntryButton from '../PrimaryNewEntryButton.svelte';
  import {BrowseParam} from '$lib/utils/search-params';
  import {pt} from '$lib/views/view-text';
  import {useViewService} from '$lib/views/view-service.svelte';
  import {SortField, type IPartOfSpeech, type IPublication, type ISemanticDomain} from '$lib/dotnet-types';
  import SortMenu from './sort/SortMenu.svelte';
  import type {SortConfig} from './sort/options';
  import {useProjectContext} from '$project/project-context.svelte';
  import type {EntryListViewMode} from './EntryListViewOptions.svelte';
  import EntryListViewOptions from './EntryListViewOptions.svelte';
  import {useProjectStorage} from '$lib/storage/project-storage.svelte';
  import ViewErrorBoundary from '$lib/layout/ViewErrorBoundary.svelte';

  const projectContext = useProjectContext();
  const viewService = useViewService();
  const dialogsService = useDialogsService();
  const features = useFeatures();
  const entryListViewMode = useProjectStorage().entryListViewMode;

  let selectedId = $state('');
  let search = $state('');
  let gridifyFilter = $state<string>();
  let publication = $state<IPublication>();
  let semanticDomain = $state<ISemanticDomain>();
  let partOfSpeech = $state<IPartOfSpeech>();
  let sort = $state<SortConfig>();
  const entryMode: EntryListViewMode = $derived(entryListViewMode.current === 'preview' ? 'preview' : 'simple');

  async function newEntry() {
    const entry = await dialogsService.createNewEntry(undefined, {
      publishIn: publication ? [publication] : [],
    }, {
      semanticDomains: semanticDomain ? [semanticDomain] : undefined,
      partOfSpeech,
    });
    if (!entry) return;
    selectedId = entry.id;
  }

  let entriesList: EntriesList | undefined = $state();

  async function onClose() {
    // Selected id is preserved by MasterDetailView so we can scroll back to it.
    await entriesList?.tryToScrollToEntry(selectedId);
  }
</script>
<Hotkey key="e" disabled={!features.write} onHotkey={() => void newEntry()} />
<SidebarPrimaryAction>
  {#snippet children(isOpen: boolean)}
    <PrimaryNewEntryButton active={!IsMobile.value && isOpen} onclick={newEntry}/>
  {/snippet}
</SidebarPrimaryAction>
<ViewErrorBoundary class="flex flex-col h-full" title={$t`Browse view failed`}>
  <MasterDetailView
    idParam={BrowseParam.EntryId}
    openParam={BrowseParam.EntryOpen}
    bind:selectedId
    onClose={onClose}
  >
    {#snippet master({selectedId: masterSelectedId, select})}
      <div class="flex flex-col h-full p-2 md:p-4 md:pr-0">
        <div class="md:mr-3">
          <SearchFilter bind:search bind:gridifyFilter bind:publication bind:semanticDomain bind:partOfSpeech />
          <div class="my-2 flex items-center justify-between">
            <SortMenu bind:value={sort}
              autoSelector={() => search ? SortField.SearchRelevance : SortField.Headword} />
            <EntryListViewOptions bind:entryMode={() => entryMode, (v) => void entryListViewMode.set(v)} />
          </div>
        </div>
        <EntriesList bind:this={entriesList}
                     {search}
                     selectedEntryId={masterSelectedId}
                     {sort}
                     {gridifyFilter}
                     {publication}
                     {partOfSpeech}
                     {semanticDomain}
                     onSelectEntry={(e) => select(e?.id ?? '')}
                     previewDictionary={entryMode === 'preview'}/>
      </div>
    {/snippet}
    {#snippet detail({selectedId: detailSelectedId, close, showClose})}
      {#if projectContext.maybeApi}
        <div class="md:p-4 md:pl-4 h-full">
          <EntryView
            entryId={detailSelectedId}
            onClose={close}
            {showClose}
          />
        </div>
      {/if}
    {/snippet}
    {#snippet empty()}
      <div class="flex items-center justify-center h-full text-muted-foreground text-center m-2">
        <p>{$t`Select ${pt($t`an entry`, $t`a word`, viewService.currentView)} to view details`}</p>
      </div>
    {/snippet}
  </MasterDetailView>
</ViewErrorBoundary>
