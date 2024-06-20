<script lang="ts">
  import {AppBar, Button, ProgressCircle} from 'svelte-ux';
  import {mdiArrowCollapseLeft, mdiArrowCollapseRight, mdiArrowLeft, mdiEyeSettingsOutline} from '@mdi/js';
  import Editor from './lib/Editor.svelte';
  import {headword, pickBestAlternative} from './lib/utils';
  import {views} from './lib/config-data';
  import {useLexboxApi} from './lib/services/service-provider';
  import type {IEntry} from './lib/mini-lcm';
  import {setContext} from 'svelte';
  import {derived, readable, writable, type Readable} from 'svelte/store';
  import {deriveAsync} from './lib/utils/time';
  import {type ViewConfig, type LexboxPermissions, type ViewOptions, type LexboxFeatures} from './lib/config-types';
  import ViewOptionsDrawer from './lib/layout/ViewOptionsDrawer.svelte';
  import EntryList from './lib/layout/EntryList.svelte';
  import Toc from './lib/layout/Toc.svelte';
  import {fade} from 'svelte/transition';
  import DictionaryEntryViewer from './lib/layout/DictionaryEntryViewer.svelte';
  import NewEntryDialog from './lib/entry-editor/NewEntryDialog.svelte';
  import SearchBar from './lib/search-bar/SearchBar.svelte';
  import ActivityView from './lib/activity/ActivityView.svelte';
  import type { OptionProvider } from './lib/services/option-provider';

  export let loading = false;

  const lexboxApi = useLexboxApi();
  const features = writable<LexboxFeatures>(lexboxApi.SupportedFeatures());
  setContext<Readable<LexboxFeatures>>('features', features);

  const permissions = writable<LexboxPermissions>({
    write: true,
    comment: true,
  });

  const options = writable<ViewOptions>({
    showExtraFields: false,
    hideEmptyFields: false,
    activeView: views[0],
    generateExternalChanges: false,
  });

  const viewConfig = derived([options, permissions, features], ([config, permissions, features]) => {
    const readonly = !permissions.write || !features.write;
    return {
      ...config,
      readonly,
      hideEmptyFields: config.hideEmptyFields || readonly,
    };
  });

  setContext<Readable<ViewConfig>>('viewConfig', viewConfig);

  export let projectName: string;
  export let isConnected: boolean;
  $: connected.set(isConnected);

  const connected = writable(false);
  const search = writable<string>('');
  setContext('listSearch', search);

  const selectedIndexExemplar = writable<string | undefined>(undefined);
  setContext('selectedIndexExamplar', selectedIndexExemplar);

  const writingSystems = deriveAsync(connected, isConnected => {
    if (!isConnected) return Promise.resolve(null);
    return lexboxApi.GetWritingSystems();
  });
  setContext('writingSystems', writingSystems);
  const indexExamplars = derived(writingSystems, wsList => {
    return wsList?.vernacular[0].exemplars;
  });
  setContext('indexExamplars', indexExamplars);
  const trigger = writable(0);
  function refreshEntries(): void {
    trigger.update(t => t + 1);
  }

  const partsOfSpeech = deriveAsync(connected, isConnected => {
    if (!isConnected) return Promise.resolve(null);
    return lexboxApi.GetPartsOfSpeech();
  });
  const semanticDomains = deriveAsync(connected, isConnected => {
    if (!isConnected) return Promise.resolve(null);
    return lexboxApi.GetSemanticDomains();
  });
  const optionProvider: OptionProvider = {
    partsOfSpeech: derived([writingSystems, partsOfSpeech], ([ws, pos]) => pos?.map(option => ({ value: option.id, label: pickBestAlternative(option.name, ws?.analysis[0]) })) ?? []),
    semanticDomains: derived([writingSystems, semanticDomains], ([ws, sd]) => sd?.map(option => ({ value: option, label: pickBestAlternative(option.name, ws?.analysis[0]) })) ?? []),
  };
  setContext('optionProvider', optionProvider);

  const _entries = deriveAsync(derived([search, connected, selectedIndexExemplar, trigger], s => s), ([s, isConnected, exemplar]) => {
    return fetchEntries(s, isConnected, exemplar);
  }, undefined, 200);

  // TODO: replace with either
  // 1 something like setContext('editorEntry') that even includes unsaved changes
  // 2 somehow use selectedEntry in components that need to refresh on changes
  // 3 combine 1 into 2
  // Used for triggering rerendering when display values of the current entry change (e.g. the headword in the list view)
  const entries = writable<IEntry[]>();
  $: $entries = $_entries;

  function fetchEntries(s: string, isConnected: boolean, exemplar: string | undefined) {
    if (!isConnected) return Promise.resolve([]);
    return lexboxApi.SearchEntries(s ?? '', {
      offset: 0,
      // we always load full exampelar lists for now, so we can guaruntee that the selected entry is in the list
      count: exemplar ? 1_000_000_000 : 1000,
      order: {field: 'headword', writingSystem: 'default'},
      exemplar: exemplar ? {value: exemplar, writingSystem: 'default'} : undefined
    });
  }

  let showOptionsDialog = false;
  const selectedEntry = writable<IEntry | undefined>(undefined);
  setContext('selectedEntry', selectedEntry);

  $: {
    $entries;
    refreshSelection();
  }

  //selection handling, make sure the selected entry is always in the list of entries
  function refreshSelection() {
    let currentEntry = $selectedEntry;
    if (currentEntry !== undefined) {
      const entry = $entries.find(e => e.id === currentEntry.id);
      if (entry !== currentEntry) {
        $selectedEntry = entry;
      }
    }
    if (!$selectedEntry && $entries?.length > 0)
      $selectedEntry = $entries[0];
  }


  $: _loading = !$entries || !$writingSystems || !$partsOfSpeech || !$semanticDomains || loading;

  function onEntryCreated(entry: IEntry) {
    $entries?.push(entry);//need to add it before refresh, otherwise it won't get selected because it's not in the list
    navigateToEntry(entry);
  }

  function navigateToEntry(entry: IEntry) {
    $search = '';
    selectEntry(entry);
  }

  function selectEntry(entry: IEntry) {
    $selectedEntry = entry;
    const indexChar: string | undefined = new Intl.Segmenter().segment(headword(entry))[Symbol.iterator]().next()?.value?.segment;
    $selectedIndexExemplar = indexChar?.toLocaleUpperCase() ?? undefined;
    refreshEntries();
    pickedEntry = true;
  }

  let expandList = false;
  let collapseActionBar = false;
  let pickedEntry = false;

  let entryActionsElem: HTMLDivElement;
  const entryActionsPortal = writable<{target: HTMLDivElement, collapsed: boolean}>();
  setContext('entryActionsPortal', entryActionsPortal);
  $: entryActionsPortal.set({target: entryActionsElem, collapsed: collapseActionBar});

</script>

<svelte:head>
  <title>{projectName}</title>
</svelte:head>

<div class="project-view !flex flex-col PortalTarget">
  <AppBar title={projectName} class="bg-secondary min-h-12" menuIcon=''>
    <div class="flex-grow-0 flex-shrink-0 md:hidden mx-2" class:invisible={!pickedEntry}>
      <Button icon={mdiArrowLeft} size="sm" iconOnly rounded variant="outline" on:click={() => pickedEntry = false} />
    </div>
    <div class="max-sm:hidden sm:flex-grow"></div>
    <div class="flex-grow-[2] mx-2">
      <SearchBar on:entrySelected={(e) => navigateToEntry(e.detail)} />
    </div>
    <div class="max-sm:hidden flex-grow-[0.25]"></div>
    <div slot="actions" class="flex items-center gap-2 sm:gap-4 whitespace-nowrap">
      {#if !$viewConfig.readonly}
        <NewEntryDialog on:created={e => onEntryCreated(e.detail.entry)} />
      {/if}
      <Button
        on:click={() => (showOptionsDialog = true)}
        size="sm"
        variant="outline"
        icon={mdiEyeSettingsOutline}>
        <div class="hidden sm:contents">
          Configure
        </div>
      </Button>
      {#if $features.history}
        <ActivityView/>
      {/if}
    </div>
  </AppBar>

  {#if _loading || !$entries}
    <div class="absolute w-full h-full z-10 bg-surface-100 flex grow items-center justify-center" out:fade={{duration: 800}}>
      <div class="inline-flex flex-col items-center text-4xl gap-4 opacity-75">
        Loading... <ProgressCircle class="text-surface-content" />
      </div>
    </div>
  {:else}
    <main class="p-4 flex grow">
      <div
        class="grid flex-grow items-start justify-stretch md:justify-center"
        style="grid-template-columns: minmax(0, min-content) minmax(0, min-content) minmax(0, min-content);"
      >
        <div class="w-screen max-w-full md:w-[500px] md:min-w-[300px] collapsible-col side-scroller" class:md:!w-[1024px]={expandList} class:md:max-w-[25vw]={!expandList} class:max-md:collapse-col={pickedEntry}>
          <EntryList bind:search={$search} entries={$entries} bind:expand={expandList} on:entrySelected={() => pickedEntry = true} />
        </div>
        <div class="max-w-full w-screen md:w-screen collapsible-col" class:md:px-6={!expandList} class:max-md:pr-6={pickedEntry && !$viewConfig.readonly} class:md:collapse-col={expandList} class:max-md:collapse-col={!pickedEntry}>
          {#if $selectedEntry}
            <div class="mb-6">
              <DictionaryEntryViewer entry={$selectedEntry} />
            </div>
            <Editor entry={$selectedEntry}
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
              {#if !$viewConfig.readonly}
                <NewEntryDialog on:created={e => onEntryCreated(e.detail.entry)}/>
              {/if}
            </div>
          {/if}
        </div>
        <div class="side-scroller h-full pl-6 border-l-2 gap-4 flex flex-col col-start-3" class:border-l-2={$selectedEntry && !expandList} class:max-md:hidden={!pickedEntry || $viewConfig.readonly}>
          <div class="hidden" class:sm:hidden={expandList}>
            <Button icon={collapseActionBar ? mdiArrowCollapseLeft : mdiArrowCollapseRight} class="aspect-square w-10" size="sm" iconOnly rounded variant="outline" on:click={() => collapseActionBar = !collapseActionBar} />
          </div>
          <div class="sm:w-[15vw] collapsible-col max-sm:self-center" class:self-center={collapseActionBar} class:collapse-col={expandList} class:w-min={collapseActionBar}>
            {#if $selectedEntry && !expandList}
              <div class="h-full flex flex-col gap-4 justify-stretch">
                {#if !$viewConfig.readonly}
                  <div class="contents" bind:this={entryActionsElem}>

                  </div>
                {/if}
                <div class="contents max-sm:hidden" class:hidden={collapseActionBar}>
                  <Toc entry={$selectedEntry} />
                </div>
              </div>
              <span class="text-surface-content bg-surface-100/75 text-sm absolute -bottom-4 -right-4 p-2 inline-flex gap-2 items-center">
                {$viewConfig.activeView.label}
                <Button
                  on:click={() => (showOptionsDialog = true)}
                  size="sm"
                  variant="default"
                  iconOnly
                  icon={mdiEyeSettingsOutline} />
              </span>
            {/if}
          </div>
        </div>
      </div>
    </main>

    <ViewOptionsDrawer bind:open={showOptionsDialog} {options} {features} />

  {/if}
</div>
