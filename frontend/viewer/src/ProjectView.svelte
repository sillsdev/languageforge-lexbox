<script lang="ts">
  import {AppBar, Button, ProgressCircle} from 'svelte-ux';
  import {mdiArrowCollapseLeft, mdiArrowCollapseRight, mdiEyeSettingsOutline} from '@mdi/js';
  import Editor from './lib/Editor.svelte';
  import {firstDefOrGlossVal, firstVal, headword} from './lib/utils';
  import {views} from './lib/config-data';
  import {useLexboxApi} from './lib/services/service-provider';
  import type {IEntry} from './lib/mini-lcm';
  import {setContext} from 'svelte';
  import {derived, writable} from 'svelte/store';
  import {deriveAsync} from './lib/utils/time';
  import type {ViewConfig} from './lib/config-types';
  import ViewOptions from './lib/layout/ViewOptions.svelte';
  import EntryList from './lib/layout/EntryList.svelte';
  import Toc from './lib/layout/Toc.svelte';
  import {fade} from 'svelte/transition';
  import DictionaryEntryViewer from './lib/layout/DictionaryEntryViewer.svelte';
  import NewEntryDialog from './lib/entry-editor/NewEntryDialog.svelte';
  import SearchBar from './lib/search-bar/SearchBar.svelte';

  export let loading = false;

  const viewConfig = writable<ViewConfig>({
    generateExternalChanges: false,
    showExtraFields: false,
    hideEmptyFields: false,
    activeView: views[0],
    readonly: undefined,
  });

  setContext('viewConfig', derived(viewConfig, (config) => ({
    ...config,
    hideEmptyFields: config.hideEmptyFields || config.readonly,
  })));
  export let isConnected: boolean;
  $: connected.set(isConnected);
  const lexboxApi = useLexboxApi();

  const connected = writable(false);
  const search = writable<string>('');


  const selectedIndexExemplar = writable<string | undefined>(undefined);
  setContext('selectedIndexExamplars', selectedIndexExemplar);

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

  const entries = deriveAsync(derived([search, connected, selectedIndexExemplar, trigger], s => s), ([s, isConnected, exemplar]) => {
    return fetchEntries(s, isConnected, exemplar);
  }, undefined, 200);

  function fetchEntries(s: string, isConnected: boolean, exemplar: string | undefined) {
    if (!isConnected) return Promise.resolve([]);
    return lexboxApi.SearchEntries(s ?? '', {
      offset: 0,
      // we always load full exampelar lists for now, so we can guaruntee that the selected entry is in the list
      count: exemplar ? Infinity : 1000,
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


  $: _loading = !$entries || !$writingSystems || loading;

  function onEntryCreated(entry: IEntry) {
    $entries?.push(entry);//need to add it before refresh, otherwise it won't get selected because it's not in the list
    selectEntry(entry);
  }

  function selectEntry(entry: IEntry) {
    $selectedEntry = entry;
    $selectedIndexExemplar = headword(entry).charAt(0).toLocaleUpperCase() || undefined;
    refreshEntries();
  }

  let expandList = false;
  let collapseActionBar = false;

  let entryActionsElem: HTMLDivElement;
  const entryActionsPortal = writable<{target: HTMLDivElement, collapsed: boolean}>();
  setContext('entryActionsPortal', entryActionsPortal);
  $: entryActionsPortal.set({target: entryActionsElem, collapsed: collapseActionBar});

</script>

<div class="app flex flex-col PortalTarget">
  <AppBar title="FLEx-Lite" class="bg-surface-300 min-h-12" menuIcon=''>
    <div class="flex-grow"></div>
    <div class="flex-grow-[2]">
      <SearchBar on:entrySelected={(e) => selectEntry(e.detail)} />
    </div>
    <div class="flex-grow-[0.25]"></div>
    <div slot="actions" class="flex items-center gap-4 whitespace-nowrap">
      {#if !$viewConfig.readonly}
        <NewEntryDialog on:created={e => onEntryCreated(e.detail.entry)} />
      {/if}
      <Button
        on:click={() => (showOptionsDialog = true)}
        size="sm"
        variant="outline"
        icon={mdiEyeSettingsOutline}>Configure</Button>
    </div>
  </AppBar>

  {#if _loading || !$entries}
    <div class="absolute w-full h-full z-10 bg-surface-100 flex grow items-center justify-center" out:fade={{duration: 800}}>
      <div class="inline-flex flex-col items-center text-4xl gap-4 opacity-75">
        Loading... <ProgressCircle class="text-surface-content" />
      </div>
    </div>
  {:else}
    <main class="p-4">
      <div
        class="grid flex-grow" class:gap-x-6={!expandList}
        style="grid-template-columns: 2fr minmax(0, min-content) minmax(0, min-content);"
      >
        <EntryList bind:search={$search} entries={$entries} bind:expand={expandList} />
        <div class="w-[65vw] max-w-[65vw] collapsible" class:collapse={expandList}>
          {#if $selectedEntry}
            <div class="mb-6">
              <DictionaryEntryViewer entry={$selectedEntry} />
            </div>
            <Editor entry={$selectedEntry}
              on:change={e => {
                $selectedEntry = $selectedEntry;
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
        {#if $selectedEntry && !expandList}
          <div class="side-scroller h-full pl-6 border-l-2 gap-4 flex flex-col col-start-3">
            {#if !expandList}
              <Button icon={collapseActionBar ? mdiArrowCollapseLeft : mdiArrowCollapseRight} class="aspect-square w-10" size="sm" iconOnly rounded variant="outline" on:click={() => collapseActionBar = !collapseActionBar} />
            {/if}
            <div class="w-[15vw] max-w-60 collapsible max-sm:self-center max-sm:w-min" class:self-center={collapseActionBar} class:collapse={expandList} class:w-min={collapseActionBar}>
              <div class="h-full flex flex-col gap-4 justify-stretch">
                {#if !$viewConfig.readonly}
                  <div class="contents" bind:this={entryActionsElem}>

                  </div>
                {/if}
                <div class="contents" class:hidden={collapseActionBar}>
                  <Toc entry={$selectedEntry} />
                </div>
              </div>
              <span class="text-surface-content bg-surface-100/75 text-sm fixed bottom-0 right-0 p-2 inline-flex gap-2 items-center">
                {$viewConfig.activeView.label}
                <Button
                  on:click={() => (showOptionsDialog = true)}
                  size="sm"
                  variant="default"
                  iconOnly
                  icon={mdiEyeSettingsOutline} />
              </span>
            </div>
          </div>
        {/if}
      </div>
    </main>

    <ViewOptions bind:open={showOptionsDialog} {viewConfig} />

  {/if}
</div>
