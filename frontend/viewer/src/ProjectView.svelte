<script lang="ts">
  import {AppBar, Button, cls, Dialog, Field, ListItem, ProgressCircle, TextField,} from 'svelte-ux';
  import {mdiCog, mdiMagnify} from '@mdi/js';
  import Editor from './lib/Editor.svelte';
  import {firstDefOrGlossVal, firstVal} from './lib/utils';
  import {views} from './lib/config-data';
  import {useLexboxApi} from './lib/services/service-provider';
  import type {IEntry, WritingSystems} from './lib/mini-lcm';
  import {onDestroy, setContext} from 'svelte';
  import {derived, writable} from 'svelte/store';
  import {deriveAsync} from './lib/utils/time';
  import type {ViewConfig} from './lib/config-types';
  import ViewOptions from './lib/layout/ViewOptions.svelte';
  import EntryList from './lib/layout/EntryList.svelte';
  import Toc from './lib/layout/Toc.svelte';
  import {fade} from 'svelte/transition';
  import DictionaryEntryViewer from './lib/layout/DictionaryEntryViewer.svelte';

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

  const entries = deriveAsync(derived([search, connected], s => s), ([s, isConnected]) => {
    if (!isConnected) return Promise.resolve([]);
    return lexboxApi.SearchEntries(s ?? '', {
      offset: 0,
      count: 1000,
      order: {field: 'headword', writingSystem: 'default'}
    });
  }, undefined, 200);

  const writingSystems = deriveAsync(connected, isConnected => {
    if (!isConnected) return Promise.resolve(null);
    return lexboxApi.GetWritingSystems();
  });
  setContext('writingSystems', writingSystems);

  let showSearchDialog = false;
  let showOptionsDialog = false;
  let selectedEntry: IEntry | undefined;

  $: _loading = !$entries || !$writingSystems || loading;
</script>

<div class="app flex flex-col h-full">
  <AppBar title="FLEx-Lite" class="bg-surface-300 min-h-12" menuIcon=''>
    <div class="flex-grow"></div>
    <Field
      classes={{input: 'my-1'}}
        on:click={() => (showSearchDialog = true)}
        class="flex-grow-[2] cursor-pointer opacity-80 hover:opacity-100"
        icon={mdiMagnify}>
        Search
    </Field>
    <div class="flex-grow-[0.25]"></div>
    <div slot="actions" class="flex items-center gap-4 whitespace-nowrap">
      <Button
        on:click={() => (showOptionsDialog = true)}
        size="sm"
        variant="outline"
        icon={mdiCog}>Configure</Button>
    </div>
  </AppBar>

  {#if _loading}
    <div class="absolute w-full h-full z-10 bg-surface-100 flex grow items-center justify-center" out:fade={{duration: 800}}>
      <div class="inline-flex flex-col items-center text-4xl gap-4 opacity-75">
        Loading... <ProgressCircle class="text-surface-content" />
      </div>
    </div>
  {:else}
    <main class="p-4">
      <div
        class="grid flex-grow gap-x-6"
        style="grid-template-columns: 2fr 4fr 1fr;"
      >
        <EntryList bind:search={$search} entries={$entries} bind:selectedEntry />
        {#if selectedEntry}
          <div>
            <div class="mb-6">
              <DictionaryEntryViewer entry={selectedEntry} />
            </div>
            <Editor bind:entry={selectedEntry} />
          </div>
          <div class="h-full min-w-48 pl-6 border-l-2 gap-4 flex flex-col">
            <div class="side-scroller h-full flex flex-col gap-4">
              <Toc entry={selectedEntry} />
            </div>
            <span class="text-surface-content text-sm fixed bottom-3 right-3 inline-flex gap-2 items-center">
              {$viewConfig.activeView.label}
              <Button
                on:click={() => (showOptionsDialog = true)}
                size="sm"
                variant="text"
                iconOnly
                icon={mdiCog} />
            </span>
          </div>
        {:else}
          <div class="w-full h-full z-10 bg-surface-100 flex grow items-center justify-center text-2xl opacity-75">
            No entry selected
          </div>
        {/if}
      </div>
    </main>

    <ViewOptions bind:open={showOptionsDialog} {viewConfig} />

    <Dialog bind:open={showSearchDialog} class="w-[700px]">
      <div slot="title">
        <TextField
          autofocus
          placeholder="Search entries"
          class="flex-grow-[2] cursor-pointer opacity-80 hover:opacity-100"
          icon={mdiMagnify}
        />
      </div>
      <div>
        {#each $entries as entry}
          <ListItem
          title={firstVal(entry.lexemeForm)}
          subheading={firstDefOrGlossVal(entry.senses[0])}
          class={cls('cursor-pointer', 'hover:bg-accent-50')}
          noShadow
        />
        {/each}
      </div>
      <div class="flex-grow"></div>
      <div slot="actions">actions</div>
    </Dialog>
  {/if}
</div>