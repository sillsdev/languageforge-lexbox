<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from './mini-lcm';
  import EntryEditor from './entry-editor/object-editors/EntryEditor.svelte';
  import type {Readable} from 'svelte/store';
  import {createEventDispatcher, getContext} from 'svelte';
  import type {ViewConfig} from './config-types';
  import jsonPatch from 'fast-json-patch';
  import {useLexboxApi} from './services/service-provider';
  import {isEmptyId} from './utils';
  import type { SaveHandler } from './services/save-event-service';

  const lexboxApi = useLexboxApi();
  const saveHandler = getContext<SaveHandler>('saveHandler');

  const dispatch = createEventDispatcher<{
    delete: { entry: IEntry };
    change: { entry: IEntry };
  }>();

  export let entry: IEntry;
  $: initialEntry = JSON.parse(JSON.stringify(entry)) as IEntry;

  function updateInitialEntry() {
    initialEntry = JSON.parse(JSON.stringify(entry)) as IEntry;
  }

  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');

  function withoutSenses(entry: IEntry): Omit<IEntry, 'senses'> {
    let {senses, ...rest} = entry;
    return rest;
  }
  function withoutExamples(sense: ISense): Omit<ISense, 'exampleSentences'> {
    let {exampleSentences, ...rest} = sense;
    return rest;
  }

  async function onChange(e: { entry: IEntry, sense?: ISense, example?: IExampleSentence }) {
    await updateEntry(e.entry);
    if (e.sense !== undefined) {
      await updateSense(e.sense);
      detectSenseIndexChanges(e.entry, e.sense);
      if (e.example !== undefined) {
        await updateExample(e.sense.id, e.example);
        detectExampleIndexChanges(e.entry, e.sense, e.example);
      }
    }

    dispatch('change', {entry: e.entry});
    updateInitialEntry();
  }

  async function onDelete(e: { entry: IEntry, sense?: ISense, example?: IExampleSentence }) {
    if (e.example !== undefined && e.sense !== undefined) {
      await saveHandler(() => lexboxApi.DeleteExampleSentence(e.entry.id, e.sense!.id, e.example!.id));
    } else if (e.sense !== undefined) {
      await saveHandler(() => lexboxApi.DeleteSense(e.entry.id, e.sense!.id));
    } else {
      await saveHandler(() => lexboxApi.DeleteEntry(e.entry.id));
      dispatch('delete', {entry: e.entry});
      return;
    }
    updateInitialEntry();
  }

  async function updateEntry(updatedEntry: IEntry) {
    if (entry.id != updatedEntry.id) throw new Error('Entry id mismatch');
    let operations = jsonPatch.compare(withoutSenses(initialEntry), withoutSenses(updatedEntry));
    if (operations.length == 0) return;
    await saveHandler(() => lexboxApi.UpdateEntry(updatedEntry.id, operations));
  }

  async function updateSense(updatedSense: ISense) {
    if (isEmptyId(updatedSense.id)) {
      updatedSense.id = crypto.randomUUID();
      await saveHandler(() => lexboxApi.CreateSense(entry.id, updatedSense));
      return;
    }
    const initialSense = initialEntry.senses.find(s => s.id === updatedSense.id);
    if (!initialSense) throw new Error('Sense not found in initial entry');
    let operations = jsonPatch.compare(withoutExamples(initialSense), withoutExamples(updatedSense));
    if (operations.length == 0) return;
    await saveHandler(() => lexboxApi.UpdateSense(entry.id, updatedSense.id, operations));
  }

  async function updateExample(senseId: string, updatedExample: IExampleSentence) {
    const initialSense = initialEntry.senses.find(s => s.id === senseId);
    if (!initialSense) throw new Error('Sense not found in initial entry');
    if (isEmptyId(updatedExample.id)) {
      updatedExample.id = crypto.randomUUID();
      await saveHandler(() => lexboxApi.CreateExampleSentence(entry.id, senseId, updatedExample));
      return;
    }
    const initialExample = initialSense.exampleSentences.find(e => e.id === updatedExample.id);
    if (!initialExample) throw new Error('Example not found in initial sense');
    let operations = jsonPatch.compare(initialExample, updatedExample);
    if (operations.length == 0) return;
    await saveHandler(() => lexboxApi.UpdateExampleSentence(entry.id, senseId, updatedExample.id, operations));
  }

  function detectSenseIndexChanges(entry: IEntry, sense: ISense) {
    const initialIndex = initialEntry.senses.findIndex(s => s.id === sense.id);
    if (initialIndex === -1) return;
    const currentIndex = entry.senses.findIndex(s => s.id === sense.id);
    if (currentIndex === -1) return;
    if (initialIndex !== currentIndex) {
      // todo figure out how to send this to the server
    }
  }

  function detectExampleIndexChanges(entry: IEntry, sense: ISense, example: IExampleSentence) {
    const initialIndex = initialEntry.senses.find(s => s.id == sense.id)?.exampleSentences.findIndex(s => s.id === example.id);
    if (initialIndex === -1 || initialIndex === undefined) return;
    const currentIndex = sense.exampleSentences.findIndex(s => s.id === example.id);
    if (currentIndex === -1) return;
    if (initialIndex !== currentIndex) {
      // todo figure out how to send this to the server
    }
  }
</script>

<div id="entry" class:hide-empty={$viewConfig.hideEmptyFields}>
  <EntryEditor
    on:change={e => onChange(e.detail)}
    on:delete={e => onDelete(e.detail)}
    entry={entry}/>
</div>

<style lang="postcss">
  :global(.hide-empty :is(.empty, .ws-field-wrapper:has(.empty))) {
    display: none !important;
  }
</style>
