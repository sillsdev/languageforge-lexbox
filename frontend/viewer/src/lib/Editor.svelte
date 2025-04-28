<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
  import EntryEditor from './entry-editor/object-editors/EntryEditor.svelte';
  import {createEventDispatcher, getContext} from 'svelte';
  import {useLexboxApi} from './services/service-provider';
  import {useSaveHandler} from './services/save-event-service.svelte';
  import {useViewSettings} from '$lib/views/view-service';

  const lexboxApi = useLexboxApi();
  const saveHandler = useSaveHandler();

  const dispatch = createEventDispatcher<{
    delete: { entry: IEntry };
    change: { entry: IEntry };
  }>();

  export let entry: IEntry;
  export let readonly: boolean;
  $: initialEntry = JSON.parse(JSON.stringify(entry)) as IEntry;

  function updateInitialEntry() {
    // eslint-disable-next-line svelte/no-reactive-reassign
    initialEntry = JSON.parse(JSON.stringify(entry)) as IEntry;
  }

  const viewSettings = useViewSettings();

  async function onChange(e: { entry: IEntry, sense?: ISense, example?: IExampleSentence }) {
    if (readonly) return;
    entry = await updateEntry(e.entry);
    dispatch('change', {entry: entry});
    updateInitialEntry();
  }

  async function onDelete(e: { entry: IEntry, sense?: ISense, example?: IExampleSentence }) {
    if (readonly) return;
    if (e.example !== undefined && e.sense !== undefined) {
      await saveHandler.handleSave(() => lexboxApi.deleteExampleSentence(e.entry.id, e.sense!.id, e.example!.id));
    } else if (e.sense !== undefined) {
      await saveHandler.handleSave(() => lexboxApi.deleteSense(e.entry.id, e.sense!.id));
    } else {
      await saveHandler.handleSave(() => lexboxApi.deleteEntry(e.entry.id));
      dispatch('delete', {entry: e.entry});
      return;
    }
    updateInitialEntry();
  }

  async function updateEntry(updatedEntry: IEntry) {
    if (entry.id != updatedEntry.id) throw new Error('Entry id mismatch');
    console.debug('Updating entry', updatedEntry);
    return saveHandler.handleSave(() => lexboxApi.updateEntry(initialEntry, updatedEntry));
  }
</script>

<div id="entry" class:hide-unused={!$viewSettings.showEmptyFields}>
  {#key entry.id}
    <EntryEditor
      on:change={e => onChange(e.detail)}
      on:delete={e => onDelete(e.detail)}
      entry={entry}
      {readonly}/>
  {/key}
</div>

<style lang="postcss">
  :global(.hide-unused :is(.unused, .ws-field-wrapper:has(.unused))) {
    display: none !important;
  }
</style>
