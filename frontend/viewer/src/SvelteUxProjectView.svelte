<script lang="ts">
  import {AppBar, Button, clamp} from 'svelte-ux';
  import {
    mdiArrowLeft,
    mdiChatQuestion,
  } from '@mdi/js';
  import Editor from './lib/Editor.svelte';
  import {useLocation} from 'svelte-routing';
  import {useFwLiteConfig, useLexboxApi} from './lib/services/service-provider';
  import type {IEntry, IMiniLcmFeatures} from './lib/dotnet-types';
  import {createEventDispatcher, onDestroy, onMount, setContext} from 'svelte';
  import {derived, writable} from 'svelte/store';
  import {deriveAsync} from './lib/utils/time';
  import type {LexboxPermissions} from './lib/config-types';
  import ViewOptionsDrawer from './lib/layout/ViewOptionsDrawer.svelte';
  import EntryList from './lib/layout/EntryList.svelte';
  import DictionaryEntryViewer from './lib/layout/DictionaryEntryViewer.svelte';
  import NewEntryDialog from './lib/entry-editor/NewEntryDialog.svelte';
  import SearchBar from './lib/search-bar/SearchBar.svelte';
  import {getAvailableHeightForElement} from './lib/utils/size';
  import {getSearchParam, getSearchParams, updateSearchParam, ViewerSearchParam} from './lib/utils/search-params';
  import SaveStatus from './lib/status/SaveStatus.svelte';
  import {saveEventDispatcher, saveHandler} from './lib/services/save-event-service';
  import {initView, initViewSettings} from './lib/views/view-service';
  import {views} from './lib/views/view-data';
  import {useEventBus} from './lib/services/event-bus';
  import {initProjectCommands, type NewEntryDialogOptions} from './lib/commands';
  import throttle from 'just-throttle';
  import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
  import DeleteDialog from '$lib/entry-editor/DeleteDialog.svelte';
  import {initDialogService} from '$lib/entry-editor/dialog-service';
  import HomeButton from '$lib/HomeButton.svelte';
  import AppBarMenu from '$lib/layout/AppBarMenu.svelte';
  import {initScottyPortalContext} from '$lib/layout/Scotty.svelte';
  import {initProjectViewState} from '$lib/views/project-view-state-service';
  import NewEntryButton from '$lib/entry-editor/NewEntryButton.svelte';
  import {getSelectedEntryChangedStore} from '$lib/services/selected-entry-service';
  import RightToolbar from '$lib/RightToolbar.svelte';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';

  const dispatch = createEventDispatcher<{
    loaded: boolean;
  }>();

  export let about: string | undefined = undefined;
  export let projectName: string;
  setContext('project-name', projectName);

  const changeEventBus = useEventBus();
  onDestroy(changeEventBus.onEntryUpdated(projectName, updateEntryInList));

  function updateEntryInList(updatedEntry: IEntry) {
    entries.update(list => {
      let updated = false;
      let updatedList = list?.map(e => {
        if (e.id === updatedEntry.id) {
          updated = true;
          return updatedEntry;
        } else {
          return e;
        }
      }) ?? [];
      //entry not found, add it to the end
      //todo this does not handle sorting, should we bother?
      if (!updated) updatedList.push(updatedEntry);

      return updatedList;
    });
  }

  const fwLiteConfig = useFwLiteConfig();
  const lexboxApi = useLexboxApi();
  setContext('saveEvents', saveEventDispatcher);
  setContext('saveHandler', saveHandler);

  const currentView = initView(views[0]);
  const viewSettings = initViewSettings();

  const permissions = writable<LexboxPermissions>({
    write: true,
    comment: true,
  });

  const features = writable<IMiniLcmFeatures>({})
  void lexboxApi.supportedFeatures().then(f => {
    features.set(f);
    $viewSettings.showEmptyFields = !!($permissions.write && $features.write);
  });

  $: readonly = !$permissions.write || !$features.write;

  const state = initProjectViewState({
    rightToolbarCollapsed: false,
    userPickedEntry: false,
  });

  export let isConnected: boolean;
  export let showHomeButton = true;
  $: connected.set(isConnected);

  const connected = writable(false);
  const search = writable<string>(getSearchParam(ViewerSearchParam.Search) ?? undefined);
  setContext('listSearch', search);
  $: updateSearchParam(ViewerSearchParam.Search, $search, true);

  const location = useLocation();
  $: {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    $location;
    const searchParams = getSearchParams();
    $search = searchParams.get(ViewerSearchParam.Search) ?? '';
    $selectedIndexExemplar = searchParams.get(ViewerSearchParam.IndexCharacter);
  }

  const selectedIndexExemplar = writable<string | null>(getSearchParam(ViewerSearchParam.IndexCharacter));
  setContext('selectedIndexExamplar', selectedIndexExemplar);
  $: updateSearchParam(ViewerSearchParam.IndexCharacter, $selectedIndexExemplar, false);

  const writingSystemService = useWritingSystemService();

  const trigger = writable(0);


  const { value: _entries, loading: loadingEntries, flush: flushLoadingEntries } =
    deriveAsync(derived([search, connected, selectedIndexExemplar, trigger], s => s), ([s, isConnected, exemplar]) => {
      return fetchEntries(s, isConnected, exemplar);
  }, undefined, 200);

  function refreshEntries(): void {
    trigger.update(t => t + 1);
    setTimeout(flushLoadingEntries);
  }

  // TODO: replace with either
  // 1 something like setContext('editorEntry') that even includes unsaved changes
  // 2 somehow use selectedEntry in components that need to refresh on changes
  // 3 combine 1 into 2
  // Used for triggering rerendering when display values of the current entry change (e.g. the headword in the list view)
  const entries = writable<IEntry[] | undefined>();
  $: $entries = $_entries;

  function fetchEntries(s: string, isConnected: boolean, exemplar: string | null): Promise<IEntry[] | undefined> {
    if (!isConnected) return Promise.resolve(undefined);
    return lexboxApi.searchEntries(s ?? '', {
      offset: 0,
      // we always load full exemplar lists for now, so we can guaruntee that the selected entry is in the list
      count: exemplar ? 1_000_000_000 : 1000,
      order: {field: SortField.Headword, writingSystem: 'default', ascending: true},
      exemplar: exemplar ? {value: exemplar, writingSystem: 'default'} : undefined
    });
  }

  let showOptionsDialog = false;
  let navigateToEntryId = getSearchParam(ViewerSearchParam.EntryId);

  // Makes the back button work for going back to the list view
  // todo use a lib for this?
  window.addEventListener('popstate', () => {
    const currEntryId = getSearchParam(ViewerSearchParam.EntryId);
    if (!currEntryId) {
      $state.userPickedEntry = false;
      $selectedEntry = undefined;
    } else if (currEntryId !== $selectedEntry?.id) {
      $state.userPickedEntry = true;
      navigateToEntryId = currEntryId;
      refreshSelection();
    }
  });

  const selectedEntry = writable<IEntry | undefined>(undefined);
  setContext('selectedEntry', selectedEntry);
  // For some reason reactive syntax doesn't pick up every change, so we need to manually subscribe
  // and we need the extra call to updateEntryIdSearchParam in refreshSelection
  const unsubSelectedEntry = selectedEntry.subscribe(updateEntryIdSearchParam);
  // eslint-disable-next-line @typescript-eslint/no-unused-expressions
  $: { $state.userPickedEntry; updateEntryIdSearchParam(); }
  function updateEntryIdSearchParam() {
    updateSearchParam(ViewerSearchParam.EntryId, navigateToEntryId ?? ($state.userPickedEntry ? $selectedEntry?.id : undefined), false);
  }

  $: {
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    $entries;
    refreshSelection();
  }

  //selection handling, make sure the selected entry is always in the list of entries
  function refreshSelection() {
    if (!$entries) return;

    if (navigateToEntryId) {
      const entry = $entries.find(e => e.id === navigateToEntryId);
      if (entry) {
        $selectedEntry = entry;
        $state.userPickedEntry = true;
      }
    } else if ($selectedEntry !== undefined) {
      const entry = $entries.find(e => e.id === $selectedEntry!.id);
      if (entry !== $selectedEntry) {
        $selectedEntry = entry;
      }
    }

    if (!$selectedEntry) {
      $state.userPickedEntry = false;
      if ($entries?.length > 0)
        $selectedEntry = $entries[0];
    }

    updateEntryIdSearchParam();
    navigateToEntryId = null;
  }

  $: projectLoaded = !!($entries && writingSystemService);
  $: dispatch('loaded', projectLoaded);

  function onEntryCreated(entry: IEntry, options?: NewEntryDialogOptions) {
    if (options?.dontSelect) return;

    $selectedEntry = entry;
    // todo the new entry might not be in the list and will be deselected
    refreshEntries();
  }

  function onEntryDeleted(event: CustomEvent<{entry: IEntry}>) {
    const _entries = $entries!;
    const deletedEntry = event.detail.entry;
    const deletedIndex = _entries.findIndex(e => e.id === deletedEntry.id);

    if (deletedIndex >= 0 && deletedIndex < _entries.length) {
      _entries.splice(deletedIndex, 1);
      $entries = _entries;
    }

    const selectIndex = clamp(deletedIndex, 0, _entries.length - 1);
    $selectedEntry = _entries[selectIndex];
  }

  function navigateToEntry(entry: IEntry, searchText?: string) {
    // this is to ensure that the selected entry is in the list of entries, otherwise it won't be selected
    $search = searchText ?? '';
    $selectedIndexExemplar = null;
    $selectedEntry = entry;
    // This just forces and flushes a refresh.
    // todo: The refresh should only be necessary if $search or $selectedIndexExemplar were actually changed
    refreshEntries();
    $state.userPickedEntry = true;
  }

  let expandList = false;

  initScottyPortalContext();

  let editorElem: HTMLElement | undefined;
  let spaceForEditorStyle: string = '';
  const updateSpaceForEditor = throttle(() => {
    if (!editorElem) return;
    const availableHeight = getAvailableHeightForElement(editorElem);
    spaceForEditorStyle = `--space-for-editor: ${availableHeight}px`;
  }, 20, { leading: false, trailing: true });

  // eslint-disable-next-line @typescript-eslint/no-unused-expressions
  $: editorElem && updateSpaceForEditor();
  onMount(() => {
    const abortController = new AbortController();
    window.addEventListener('resize', updateSpaceForEditor, abortController);
    window.addEventListener('scroll', updateSpaceForEditor, abortController);
    return () => {
      abortController.abort();
      unsubSelectedEntry();
    };
  });

  const selectedEntryChanged = getSelectedEntryChangedStore(selectedEntry);
  onDestroy(selectedEntryChanged.subscribe(() => { // reactive syntax was not reliable
    const scrolledDown = (editorElem?.getBoundingClientRect()?.y ?? 0) < 0;
    // we don't want to scroll the app-bar out of view, but we also don't want to scroll it into view
    // i.e. the project-view should look the same, we just want to make sure we're at the top of the editor
    if (scrolledDown) editorElem?.scrollIntoView({block: 'start', inline: 'nearest', behavior: 'instant'});
  }));

  let newEntryDialog: NewEntryDialog;
  async function openNewEntryDialog(lexemeForm?: string, options?: NewEntryDialogOptions): Promise<IEntry | undefined> {
    const partialEntry: Partial<IEntry> = {};
    if (lexemeForm) {
      const defaultWs = writingSystemService.defaultVernacular()?.wsId;
      if (defaultWs === undefined) return undefined;
      partialEntry.lexemeForm = {[defaultWs]: lexemeForm};
    }
    const entry = await newEntryDialog.openWithValue(partialEntry);
    if (entry) onEntryCreated(entry, options);
    return entry;
  }
  let deleteDialog: DeleteDialog;
  $: dialogHolder.dialog = deleteDialog;
  const dialogHolder: {dialog?: DeleteDialog} = {};
  initDialogService(() => dialogHolder.dialog);

  initProjectCommands({
    createNewEntry: openNewEntryDialog,
  });
</script>

<svelte:head>
  <title>{projectName}</title>
</svelte:head>


{#if projectLoaded}
{#if !readonly}
  <NewEntryDialog bind:this={newEntryDialog} />
{/if}
<div class="project-view !flex flex-col PortalTarget" style={spaceForEditorStyle}>
  <AppBar class="bg-primary/25 min-h-12 shadow-md sm-view:sticky sm-view:top-0 overflow-hidden" head={false}>
    <div slot="title" class="prose whitespace-nowrap max-w-[20%] sm-view:hidden">
      <h3 class="text-ellipsis overflow-hidden">{projectName}</h3>
    </div>
    <div slot="menuIcon" class="contents" class:hidden={!showHomeButton}>
      <div class="contents" class:sm-view:hidden={$state.userPickedEntry}>
        <HomeButton />
      </div>
      <div class="hidden" class:sm-view:contents={$state.userPickedEntry}>
        <Button icon={mdiArrowLeft} on:click={() => $state.userPickedEntry = false} />
      </div>
    </div>
    {#if $features.write}
      <div class="inline-flex flex-grow-0 basis-40 max-sm:hidden mx-2 sm-view:basis-10">
        <SaveStatus />
      </div>
    {/if}

    <div class="sm:flex-grow"></div>
    <div class="flex-grow-[2] mx-2 sm-view:overflow-hidden">
      <SearchBar on:entrySelected={(e) => navigateToEntry(e.detail.entry, e.detail.search)}
                 {projectName}
                 createNew={newEntryDialog !== undefined}
                 on:createNew={(e) => openNewEntryDialog(e.detail)} />
    </div>
    <div class="max-sm:hidden flex-grow"></div>
    <div slot="actions" class="flex items-center whitespace-nowrap">
      <div class="space-x-2">
        {#if !readonly}
          <NewEntryButton on:click={() => openNewEntryDialog()} />
        {/if}
        {#if $features.feedback && fwLiteConfig.feedbackUrl}
          <Button
            href={fwLiteConfig.feedbackUrl}
            target="_blank"
            size="sm"
            variant="outline"
            icon={mdiChatQuestion}>
            <div class="hidden sm:contents">
              Feedback
            </div>
          </Button>
        {/if}
      </div>
      <div class="ml-2">
        <AppBarMenu on:showOptionsDialog={() => showOptionsDialog = true} {about} {projectName} />
      </div>
    </div>
  </AppBar>
  <main bind:this={editorElem} class="lg-view:p-4 flex grow" class:sm-view:p-2={$state.userPickedEntry}>
    <div
      class="grid flex-grow items-start justify-stretch lg-view:justify-center"
      style="grid-template-columns: minmax(0, min-content) minmax(0, min-content) minmax(0, min-content);"
    >
      <div class="w-screen max-w-full lg-view:w-[500px] lg-view:min-w-[300px] collapsible-col lg-view:side-scroller flex" class:lg-view:!w-[1024px]={expandList} class:lg-view:max-w-[25vw]={!expandList} class:sm-view:collapse-col={$state.userPickedEntry}>
        <EntryList bind:search={$search} entries={$entries} loading={$loadingEntries} bind:expand={expandList} on:entrySelected={() => $state.userPickedEntry = true} />
      </div>
      <div class="max-w-full w-screen lg-view:w-screen collapsible-col overflow-x-visible" class:lg-view:px-6={!expandList} class:lg-view:collapse-col={expandList} class:sm-view:collapse-col={!$state.userPickedEntry}>
        {#if $selectedEntry}
          <div class="sm-form:mb-4 mb-6">
            <DictionaryEntryViewer entry={$selectedEntry} />
          </div>
          <Editor entry={$selectedEntry}
                  {readonly}
            on:change={(e) => {
              updateEntryInList($selectedEntry = e.detail.entry);
              $entries = $entries;
            }}
            on:delete={onEntryDeleted} />
        {:else}
          <div class="w-full h-full z-10 bg-surface-100 flex flex-col gap-4 grow items-center justify-center text-2xl opacity-75">
            No entry selected
            {#if !readonly}
              <NewEntryButton on:click={() => openNewEntryDialog()} />
            {/if}
          </div>
        {/if}
      </div>
      <RightToolbar selectedEntry={$selectedEntry} {expandList} on:showOptionsDialog={() => showOptionsDialog = true} />
    </div>
  </main>

  <ViewOptionsDrawer bind:open={showOptionsDialog} bind:activeView={$currentView} bind:viewSettings={$viewSettings} bind:features={$features} />
</div>
{/if}
<DeleteDialog bind:this={deleteDialog} />
