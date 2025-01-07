<script lang="ts">
  import {AppBar, Button, ProgressCircle} from 'svelte-ux';
  import {
    mdiArrowCollapseRight,
    mdiArrowExpandLeft,
    mdiArrowLeft,
    mdiChatQuestion,
    mdiEyeSettingsOutline,
    mdiHome
  } from '@mdi/js';
  import Editor from './lib/Editor.svelte';
  import {navigate, useLocation} from 'svelte-routing';
  import {headword} from './lib/utils';
  import {useLexboxApi} from './lib/services/service-provider';
  import type {IEntry} from './lib/dotnet-types';
  import {onDestroy, onMount, setContext} from 'svelte';
  import {derived, type Readable, writable} from 'svelte/store';
  import {deriveAsync} from './lib/utils/time';
  import {type LexboxFeatures, type LexboxPermissions} from './lib/config-types';
  import ViewOptionsDrawer from './lib/layout/ViewOptionsDrawer.svelte';
  import EntryList from './lib/layout/EntryList.svelte';
  import Toc from './lib/layout/Toc.svelte';
  import {fade} from 'svelte/transition';
  import DictionaryEntryViewer from './lib/layout/DictionaryEntryViewer.svelte';
  import NewEntryDialog from './lib/entry-editor/NewEntryDialog.svelte';
  import SearchBar from './lib/search-bar/SearchBar.svelte';
  import ActivityView from './lib/activity/ActivityView.svelte';
  import {getAvailableHeightForElement} from './lib/utils/size';
  import {getSearchParam, getSearchParams, updateSearchParam, ViewerSearchParam} from './lib/utils/search-params';
  import SaveStatus from './lib/status/SaveStatus.svelte';
  import {saveEventDispatcher, saveHandler} from './lib/services/save-event-service';
  import {AppNotification} from './lib/notifications/notifications';
  import flexLogo from './lib/assets/flex-logo.png';
  import {initView, initViewSettings} from './lib/services/view-service';
  import {views} from './lib/entry-editor/view-data';
  import {initWritingSystems} from './lib/writing-systems';
  import {useEventBus} from './lib/services/event-bus';
  import AboutDialog from './lib/about/AboutDialog.svelte';
  import {initProjectCommands, type NewEntryDialogOptions} from './lib/commands';
  import throttle from 'just-throttle';
  import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
  import DeleteDialog from '$lib/entry-editor/DeleteDialog.svelte';
  import {initDialogService} from '$lib/entry-editor/dialog-service';

  export let loading = false;
  export let about: string | undefined = undefined;

  const changeEventBus = useEventBus();
  onDestroy(changeEventBus.onEntryUpdated(updatedEntry => {
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
  }));

  const lexboxApi = useLexboxApi();
  void lexboxApi.supportedFeatures().then(f => {
    features.set(f);
  });
  //not having write enabled at the start fixes an issue where the default viewSetting.hideEmptyFields would be incorrect
  const features = writable<LexboxFeatures>({write: true});
  setContext<Readable<LexboxFeatures>>('features', features);
  setContext('saveEvents', saveEventDispatcher);
  setContext('saveHandler', saveHandler);

  const permissions = writable<LexboxPermissions>({
    write: true,
    comment: true,
  });

  $: readonly = !$permissions.write || !$features.write;

  const currentView = initView(views[0]);
  const viewSettings = initViewSettings({hideEmptyFields: !$permissions.write || !$features.write});

  export let projectName: string;
  setContext('project-name', projectName);
  export let isConnected: boolean;
  export let showHomeButton = true;
  $: connected.set(isConnected);

  const connected = writable(false);
  const search = writable<string>(getSearchParam(ViewerSearchParam.Search) ?? undefined);
  setContext('listSearch', search);
  $: updateSearchParam(ViewerSearchParam.Search, $search, true);

  const location = useLocation();
  $: {
    $location;
    const searchParams = getSearchParams();
    $search = searchParams.get(ViewerSearchParam.Search) ?? '';
    $selectedIndexExemplar = searchParams.get(ViewerSearchParam.IndexCharacter);
    navigateToEntryId = searchParams.get(ViewerSearchParam.EntryId);
  }

  const selectedIndexExemplar = writable<string | null>(getSearchParam(ViewerSearchParam.IndexCharacter));
  setContext('selectedIndexExamplar', selectedIndexExemplar);
  $: updateSearchParam(ViewerSearchParam.IndexCharacter, $selectedIndexExemplar, false);

  const writingSystems = initWritingSystems(deriveAsync(connected, isConnected => {
    if (!isConnected) return Promise.resolve(null);
    return lexboxApi.getWritingSystems();
  }).value);
  const indexExamplars = derived(writingSystems, wsList => {
    return wsList?.vernacular[0].exemplars;
  });
  setContext('indexExamplars', indexExamplars);
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
  $: console.debug('Entries:', $_entries);

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
  let pickedEntry = false;
  let navigateToEntryId = getSearchParam(ViewerSearchParam.EntryId);

  // Makes the back button work for going back to the list view
  // todo use a lib for this?
  window.addEventListener('popstate', () => {
    const currEntryId = getSearchParam(ViewerSearchParam.EntryId);
    if (!currEntryId) {
      pickedEntry = false;
      $selectedEntry = undefined;
    } else {
      if (currEntryId !== $selectedEntry?.id) {
        navigateToEntryId = currEntryId;
        refreshSelection();
      }
    }
  });

  const selectedEntry = writable<IEntry | undefined>(undefined);
  setContext('selectedEntry', selectedEntry);
  // For some reason reactive syntax doesn't pick up every change, so we need to manually subscribe
  // and we need the extra call to updateEntryIdSearchParam in refreshSelection
  const unsubSelectedEntry = selectedEntry.subscribe(updateEntryIdSearchParam);
  $: { pickedEntry; updateEntryIdSearchParam(); }
  function updateEntryIdSearchParam() {
    updateSearchParam(ViewerSearchParam.EntryId, navigateToEntryId ?? (pickedEntry ? $selectedEntry?.id : undefined));
  }

  $: {
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
        pickedEntry = true;
      }
    } else if ($selectedEntry !== undefined) {
      const entry = $entries.find(e => e.id === $selectedEntry!.id);
      if (entry !== $selectedEntry) {
        $selectedEntry = entry;
      }
    }

    if (!$selectedEntry) {
      pickedEntry = false;
      if ($entries?.length > 0)
        $selectedEntry = $entries[0];
    }

    updateEntryIdSearchParam();
    navigateToEntryId = null;
  }

  $: _loading = !$entries || !$writingSystems || loading;

  function onEntryCreated(entry: IEntry, options?: NewEntryDialogOptions) {
    $entries?.push(entry);//need to add it before refresh, otherwise it won't get selected because it's not in the list
    if (!options?.dontNavigate) {
      navigateToEntry(entry, headword(entry));
    }
  }

  function onEntryDeleted(event: CustomEvent<{entry: IEntry}>) {
    const _entries = $entries!;
    const deletedEntry = event.detail.entry;
    const deletedIndex = _entries.findIndex(e => e.id === deletedEntry.id);
    $selectedEntry = _entries[deletedIndex + 1];

    if (deletedIndex >= 0 && deletedIndex < _entries.length) {
      _entries.splice(deletedIndex, 1);
      $entries = _entries;
    }
  }

  function navigateToEntry(entry: IEntry, searchText?: string) {
    // this is to ensure that the selected entry is in the list of entries, otherwise it won't be selected
    $search = searchText ?? '';
    $selectedIndexExemplar = null;
    $selectedEntry = entry;
    // This just forces and flushes a refresh.
    // todo: The refresh should only be necessary if $search or $selectedIndexExemplar were actually changed
    refreshEntries();
    pickedEntry = true;
  }

  let expandList = false;
  let collapseActionBar = false;

  let entryActionsElem: HTMLDivElement;
  const entryActionsPortal = writable<{target: HTMLDivElement, collapsed: boolean}>();
  setContext('entryActionsPortal', entryActionsPortal);
  $: entryActionsPortal.set({target: entryActionsElem, collapsed: collapseActionBar});

  let editorElem: HTMLElement | undefined;
  let spaceForEditorStyle: string = '';
  const updateSpaceForEditor = throttle(() => {
    if (!editorElem) return;
    const availableHeight = getAvailableHeightForElement(editorElem);
    spaceForEditorStyle = `--space-for-editor: ${availableHeight}px`;
  }, 20, { leading: false, trailing: true });

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

  function openInFlex() {
    AppNotification.displayAction('The project is open in FieldWorks. Please close it to reopen.', 'warning', {
      label: 'Open',
      callback: () => window.location.reload()
    });
  }

  let newEntryDialog: NewEntryDialog;
  async function openNewEntryDialog(text: string, options?: NewEntryDialogOptions): Promise<IEntry | undefined> {
    const defaultWs = $writingSystems?.vernacular[0].wsId;
    if (defaultWs === undefined) return undefined;
    const entry = await newEntryDialog.openWithValue({lexemeForm: {[defaultWs]: text}});
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


{#if _loading || !$entries}
<div class="absolute w-full h-full z-10 bg-surface-100 flex grow items-center justify-center" out:fade={{duration: 800}}>
  <div class="inline-flex flex-col items-center text-4xl gap-4 opacity-75">
    <span>Loading <span class="text-primary-500">{projectName}</span>...</span><ProgressCircle class="text-surface-content" />
  </div>
</div>
{:else}
<div class="project-view !flex flex-col PortalTarget" style={spaceForEditorStyle}>
  <AppBar class="bg-secondary min-h-12 shadow-md" head={false}>
    <div slot="title" class="prose whitespace-nowrap">
      <h3>{projectName}</h3>
    </div>
    <Button
      classes={{root: showHomeButton ? '' : 'hidden'}}
      slot="menuIcon"
      icon={mdiHome}
      on:click={() => navigate('/')}
    />
    <div class="flex-grow-0 flex-shrink-0 lg-view:hidden ml-2" class:invisible={!pickedEntry}>
      <Button icon={mdiArrowLeft} size="sm" iconOnly rounded variant="outline" on:click={() => pickedEntry = false} />
    </div>
    {#if $features.write}
      <div class="inline-flex flex-grow-0 basis-40 max-sm:hidden mx-2 sm-view:basis-10">
        <SaveStatus />
      </div>
    {/if}

    <div class="sm:flex-grow"></div>
    <div class="flex-grow-[2] mx-2">
      <SearchBar on:entrySelected={(e) => navigateToEntry(e.detail.entry, e.detail.search)}
                 createNew={newEntryDialog !== undefined}
                 on:createNew={(e) => openNewEntryDialog(e.detail)} />
    </div>
    <div class="max-sm:hidden flex-grow"></div>
    <div slot="actions" class="flex items-center gap-2 lg-view:gap-4 whitespace-nowrap">
      {#if !readonly}
        <NewEntryDialog bind:this={newEntryDialog} />
      {/if}
      {#if $features.history}
        <ActivityView {projectName}/>
      {/if}
      {#if about}
        <AboutDialog text={about} />
      {/if}
      {#if $features.feedback}
        <Button
          href="/api/feedback/fw-lite"
          target="_blank"
          size="sm"
          variant="outline"
          icon={mdiChatQuestion}>
          <div class="hidden sm:contents">
            Feedback
          </div>
        </Button>
      {/if}
      <Button
        on:click={() => (showOptionsDialog = true)}
        size="sm"
        variant="outline"
        icon={mdiEyeSettingsOutline}>
        <div class="hidden lg-view:contents">
          Configure
        </div>
      </Button>
    </div>
  </AppBar>
  <main bind:this={editorElem} class="p-4 flex grow">
    <div
      class="grid flex-grow items-start justify-stretch lg-view:justify-center"
      style="grid-template-columns: minmax(0, min-content) minmax(0, min-content) minmax(0, min-content);"
    >
      <div class="w-screen max-w-full lg-view:w-[500px] lg-view:min-w-[300px] collapsible-col side-scroller flex" class:lg-view:!w-[1024px]={expandList} class:lg-view:max-w-[25vw]={!expandList} class:sm-view:collapse-col={pickedEntry}>
        <EntryList bind:search={$search} entries={$entries} loading={$loadingEntries} bind:expand={expandList} on:entrySelected={() => pickedEntry = true} />
      </div>
      <div class="max-w-full w-screen lg-view:w-screen collapsible-col overflow-x-visible" class:lg-view:px-6={!expandList} class:sm-view:pr-6={pickedEntry && !readonly} class:lg-view:collapse-col={expandList} class:sm-view:collapse-col={!pickedEntry}>
        {#if $selectedEntry}
          <div class="sm-form:mb-4 mb-6">
            <DictionaryEntryViewer entry={$selectedEntry} />
          </div>
          <Editor entry={$selectedEntry}
                  {readonly}
            on:change={_ => {
              $selectedEntry = $selectedEntry;
              $entries = $entries;
            }}
            on:delete={onEntryDeleted} />
        {:else}
          <div class="w-full h-full z-10 bg-surface-100 flex flex-col gap-4 grow items-center justify-center text-2xl opacity-75">
            No entry selected
            {#if !readonly}
              <NewEntryDialog on:created={e => onEntryCreated(e.detail.entry)}/>
            {/if}
          </div>
        {/if}
      </div>
      <div class="side-scroller pl-6 border-l-2 gap-4 flex flex-col col-start-3" class:border-l-2={$selectedEntry && !expandList} class:sm-view:border-l-2={pickedEntry && !readonly} class:sm-view:hidden={!pickedEntry || readonly} class:lg-view:hidden={expandList}>
        {#if $selectedEntry}
          <div class="sm-form:hidden" class:sm:hidden={expandList}>
            <Button icon={collapseActionBar ? mdiArrowExpandLeft : mdiArrowCollapseRight} class="text-field-sibling-button" iconOnly rounded variant="outline" on:click={() => collapseActionBar = !collapseActionBar} />
          </div>
        {/if}
        <div class="sm-form:w-auto w-[15vw] collapsible-col max-sm:self-center" class:self-center={collapseActionBar} class:lg-view:collapse-col={expandList} class:!w-min={collapseActionBar}>
          {#if $selectedEntry}
            <div class="contents" class:lg-view:hidden={expandList}>
              <div class="h-full flex flex-col gap-4 justify-stretch">
                {#if !readonly}
                  <div class="contents" bind:this={entryActionsElem}>

                  </div>
                {/if}
                {#if $features.openWithFlex && $selectedEntry}
                  <div class="contents">
<!--                    button must be a link otherwise it won't follow the redirect to a protocol handler-->
                    <Button
                      href={`/api/fw/${projectName}/open/entry/${$selectedEntry.id}`}
                      on:click={openInFlex}
                      variant="fill-light"
                      color="info"
                      size="sm">
                      <img src={flexLogo} alt="FieldWorks logo" class="h-6 max-w-fit"/>
                      <div class="sm-form:hidden" class:hidden={$entryActionsPortal.collapsed}>
                        Open in FieldWorks
                      </div>
                    </Button>
                  </div>
                {/if}
                <div class="contents sm-form:hidden" class:hidden={collapseActionBar}>
                  <Toc entry={$selectedEntry} />
                </div>
              </div>
              <span class="text-surface-content bg-surface-100/75 text-sm absolute -bottom-4 -right-4 p-2 inline-flex gap-2 text-end items-center">
                {$currentView.label}
                <Button
                  on:click={() => (showOptionsDialog = true)}
                  size="sm"
                  variant="default"
                  iconOnly
                  icon={mdiEyeSettingsOutline} />
              </span>
            </div>
          {/if}
        </div>
      </div>
    </div>
  </main>

  <ViewOptionsDrawer bind:open={showOptionsDialog} bind:activeView={$currentView} bind:viewSettings={$viewSettings} bind:features={$features} />
</div>
{/if}
<DeleteDialog bind:this={deleteDialog} />
