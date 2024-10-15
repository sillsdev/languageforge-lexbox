<script lang="ts">
  import {AppBar, Button, ProgressCircle} from 'svelte-ux';
  import {
    mdiArrowCollapseLeft,
    mdiArrowCollapseRight,
    mdiArrowLeft,
    mdiChatQuestion,
    mdiEyeSettingsOutline,
    mdiHome
  } from '@mdi/js';
  import Editor from './lib/Editor.svelte';
  import {navigate} from 'svelte-routing';
  import {headword, pickBestAlternative} from './lib/utils';
  import {useLexboxApi} from './lib/services/service-provider';
  import type {IEntry} from './lib/mini-lcm';
  import {onDestroy, onMount, setContext} from 'svelte';
  import {derived, writable, type Readable} from 'svelte/store';
  import {deriveAsync, makeDebouncer} from './lib/utils/time';
  import { type LexboxPermissions, type LexboxFeatures} from './lib/config-types';
  import ViewOptionsDrawer from './lib/layout/ViewOptionsDrawer.svelte';
  import EntryList from './lib/layout/EntryList.svelte';
  import Toc from './lib/layout/Toc.svelte';
  import {fade} from 'svelte/transition';
  import DictionaryEntryViewer from './lib/layout/DictionaryEntryViewer.svelte';
  import NewEntryDialog from './lib/entry-editor/NewEntryDialog.svelte';
  import SearchBar from './lib/search-bar/SearchBar.svelte';
  import ActivityView from './lib/activity/ActivityView.svelte';
  import type { OptionProvider } from './lib/services/option-provider';
  import { getAvailableHeightForElement } from './lib/utils/size';
  import { ViewerSearchParam, getSearchParam, updateSearchParam } from './lib/utils/search-params';
  import SaveStatus from './lib/status/SaveStatus.svelte';
  import { saveEventDispatcher, saveHandler } from './lib/services/save-event-service';
  import {AppNotification} from './lib/notifications/notifications';
  import flexLogo from './lib/assets/flex-logo.png';
  import {initView, initViewSettings} from './lib/services/view-service';
  import {views} from './lib/entry-editor/view-data';
  import {initWritingSystems} from './lib/writing-systems';
  import {useEventBus} from './lib/services/event-bus';
  import AboutDialog from './lib/about/AboutDialog.svelte';

  export let loading = false;

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
  const features = writable<LexboxFeatures>(lexboxApi.SupportedFeatures());
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
  const search = writable<string>(getSearchParam(ViewerSearchParam.Search));
  setContext('listSearch', search);
  $: updateSearchParam(ViewerSearchParam.Search, $search, true);

  //todo listen for changes to the url, for example when back/forward is pressed
  const selectedIndexExemplar = writable<string | undefined>(getSearchParam(ViewerSearchParam.IndexCharacter));
  setContext('selectedIndexExamplar', selectedIndexExemplar);
  $: updateSearchParam(ViewerSearchParam.IndexCharacter, $selectedIndexExemplar, false);

  const writingSystems = initWritingSystems(deriveAsync(connected, isConnected => {
    if (!isConnected) return Promise.resolve(null);
    return lexboxApi.GetWritingSystems();
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

  function fetchEntries(s: string, isConnected: boolean, exemplar: string | undefined) {
    if (!isConnected) return Promise.resolve(undefined);
    return lexboxApi.SearchEntries(s ?? '', {
      offset: 0,
      // we always load full exampelar lists for now, so we can guaruntee that the selected entry is in the list
      count: exemplar ? 1_000_000_000 : 1000,
      order: {field: 'headword', writingSystem: 'default'},
      exemplar: exemplar ? {value: exemplar, writingSystem: 'default'} : undefined
    });
  }

  let showOptionsDialog = false;
  let pickedEntry = false;
  let navigateToEntryIdOnLoad = getSearchParam(ViewerSearchParam.EntryId);
  const selectedEntry = writable<IEntry | undefined>(undefined);
  setContext('selectedEntry', selectedEntry);
  // For some reason reactive syntax doesn't pick up every change, so we need to manually subscribe
  // and we need the extra call to updateEntryIdSearchParam in refreshSelection
  const unsubSelectedEntry = selectedEntry.subscribe(updateEntryIdSearchParam);
  $: { pickedEntry; updateEntryIdSearchParam(); }
  function updateEntryIdSearchParam() {
    updateSearchParam(ViewerSearchParam.EntryId, navigateToEntryIdOnLoad ?? (pickedEntry ? $selectedEntry?.id : undefined), true);
  }

  $: {
    $entries;
    refreshSelection();
  }

  //selection handling, make sure the selected entry is always in the list of entries
  function refreshSelection() {
    if (!$entries) return;

    if ($selectedEntry !== undefined) {
      const entry = $entries.find(e => e.id === $selectedEntry!.id);
      if (entry !== $selectedEntry) {
        $selectedEntry = entry;
      }
    } else if (navigateToEntryIdOnLoad) {
      const entry = $entries.find(e => e.id === navigateToEntryIdOnLoad);
      if (entry) {
        $selectedEntry = entry;
      }
    }

    if ($selectedEntry) {
      pickedEntry = true;
    } else {
      pickedEntry = false;
      if ($entries?.length > 0)
        $selectedEntry = $entries[0];
    }

    updateEntryIdSearchParam();
    navigateToEntryIdOnLoad = undefined;
  }

  $: _loading = !$entries || !$writingSystems || loading;

  function onEntryCreated(entry: IEntry) {
    $entries?.push(entry);//need to add it before refresh, otherwise it won't get selected because it's not in the list
    navigateToEntry(entry, headword(entry));
  }

  function navigateToEntry(entry: IEntry, searchText?: string) {
    // this is to ensure that the selected entry is in the list of entries, otherwise it won't be selected
    $search = searchText ?? '';
    $selectedIndexExemplar = undefined;
    $selectedEntry = entry;
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
  const updateSpaceForEditor = makeDebouncer(() => {
    if (!editorElem) return;
    const availableHeight = getAvailableHeightForElement(editorElem);
    spaceForEditorStyle = `--space-for-editor: ${availableHeight}px`;
  }, 30).debounce;

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

  async function openInFlex() {
    AppNotification.displayAction('The project is open in FieldWorks. Please close it to reopen.', 'warning', {
      label: 'Open',
      callback: () => window.location.reload()
    });
  }
  let newEntryDialog: NewEntryDialog;
  function openNewEntryDialog(text: string) {
    const defaultWs = $writingSystems?.vernacular[0].id;
    if (defaultWs === undefined) return;
    newEntryDialog.openWithValue({lexemeForm: {[defaultWs]: text}});
  }
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
<div class="project-view !flex flex-col PortalTarget" style="{spaceForEditorStyle}">
  <AppBar title={projectName} class="bg-secondary min-h-12 shadow-md">
    <Button
      classes={{root: showHomeButton ? '' : 'hidden'}}
      slot="menuIcon"
      icon={mdiHome}
      on:click={() => navigate('/')}
    />
    <div class="flex-grow-0 flex-shrink-0 lg:hidden mx-2 sm:mr-0" class:invisible={!pickedEntry}>
      <Button icon={mdiArrowLeft} size="sm" iconOnly rounded variant="outline" on:click={() => pickedEntry = false} />
    </div>
    {#if $features.write}
      <div class="inline-flex flex-grow-0 basis-60 max-sm:hidden mx-2">
        <SaveStatus />
      </div>
    {/if}

    <div class="max-sm:hidden sm:flex-grow"></div>
    <div class="flex-grow-[2] mx-2">
      <SearchBar on:entrySelected={(e) => navigateToEntry(e.detail.entry, e.detail.search)}
                 createNew={newEntryDialog !== undefined}
                 on:createNew={(e) => openNewEntryDialog(e.detail)} />
    </div>
    <div class="max-sm:hidden flex-grow"></div>
    <div slot="actions" class="flex items-center gap-2 sm:gap-4 whitespace-nowrap">
      {#if !readonly}
        <NewEntryDialog bind:this={newEntryDialog} on:created={e => onEntryCreated(e.detail.entry)} />
      {/if}
      <Button
        on:click={() => (showOptionsDialog = true)}
        size="sm"
        variant="outline"
        icon={mdiEyeSettingsOutline}>
        <div class="hidden md:contents">
          Configure
        </div>
      </Button>
      {#if $features.history}
        <ActivityView {projectName}/>
      {/if}
      {#if $features.about}
        <AboutDialog text={$features.about} />
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
    </div>
  </AppBar>
  <main bind:this={editorElem} class="p-4 flex grow">
    <div
      class="grid flex-grow items-start justify-stretch md:justify-center"
      style="grid-template-columns: minmax(0, min-content) minmax(0, min-content) minmax(0, min-content);"
    >
      <div class="w-screen max-w-full lg:w-[500px] lg:min-w-[300px] collapsible-col side-scroller flex" class:lg:!w-[1024px]={expandList} class:lg:max-w-[25vw]={!expandList} class:max-lg:collapse-col={pickedEntry}>
        <EntryList bind:search={$search} entries={$entries} loading={$loadingEntries} bind:expand={expandList} on:entrySelected={() => pickedEntry = true} />
      </div>
      <div class="max-w-full w-screen lg:w-screen collapsible-col" class:lg:px-6={!expandList} class:max-lg:pr-6={pickedEntry && !readonly} class:lg:collapse-col={expandList} class:max-lg:collapse-col={!pickedEntry}>
        {#if $selectedEntry}
          <div class="mb-6">
            <DictionaryEntryViewer entry={$selectedEntry} />
          </div>
          <Editor entry={$selectedEntry}
                  {readonly}
            on:change={e => {
              $selectedEntry = $selectedEntry;
              $entries = $entries;
            }}
            on:delete={e => {
              $selectedEntry = undefined;
              refreshEntries();
            }} />
        {:else}
          <div class="w-full h-full z-10 bg-surface-100 flex flex-col gap-4 grow items-center justify-center text-2xl opacity-75">
            No entry selected
            {#if !readonly}
              <NewEntryDialog on:created={e => onEntryCreated(e.detail.entry)}/>
            {/if}
          </div>
        {/if}
      </div>
      <div class="side-scroller pl-6 border-l-2 gap-4 flex flex-col col-start-3" class:border-l-2={$selectedEntry && !expandList} class:max-lg:border-l-2={pickedEntry && !readonly} class:max-lg:hidden={!pickedEntry || readonly} class:lg:hidden={expandList}>
        <div class="hidden" class:sm:hidden={expandList}>
          <Button icon={collapseActionBar ? mdiArrowCollapseLeft : mdiArrowCollapseRight} class="aspect-square w-10" size="sm" iconOnly rounded variant="outline" on:click={() => collapseActionBar = !collapseActionBar} />
        </div>
        <div class="sm:w-[15vw] collapsible-col max-sm:self-center" class:self-center={collapseActionBar} class:lg:collapse-col={expandList} class:w-min={collapseActionBar}>
          {#if $selectedEntry}
            <div class="contents" class:lg:hidden={expandList}>
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
                      <img src={flexLogo} alt="FieldWorks logo" class="h-6"/>
                      <div class="hidden" class:sm:contents={!$entryActionsPortal.collapsed}>
                        Open in FieldWorks
                      </div>
                    </Button>
                  </div>
                {/if}
                <div class="contents max-sm:hidden" class:hidden={collapseActionBar}>
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
