<script lang="ts">
  import type {IEntry, IExampleSentence, ISense} from './mini-lcm';
  import EntryEditor from './entry-editor/EntryEditor.svelte';
  import type {Readable} from 'svelte/store';
  import {getContext} from 'svelte';
  import type {ViewConfig} from './config-types';
  import jsonPatch from 'fast-json-patch';
  import {useLexboxApi} from './services/service-provider';

  let lexboxApi = useLexboxApi();

  export let entry: IEntry;
  $: initialEntry = JSON.parse(JSON.stringify(entry)) as IEntry;


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
      if (e.example !== undefined) {
        await updateExample(e.sense.id, e.example);
      }
    }

    entry = entry;//trigger initial entry update
  }

  async function onCreate(e: { entry: IEntry, sense: ISense, example?: IExampleSentence }) {
    if (e.example) {
      await lexboxApi.CreateExampleSentence(entry.id, e.sense.id, e.example);
    } else {
      await lexboxApi.CreateSense(entry.id, e.sense);
    }

    entry = entry;//trigger entry update
  }

  async function updateEntry(updatedEntry: IEntry) {
    if (entry.id != updatedEntry.id) throw new Error('Entry id mismatch');
    let operations = jsonPatch.compare(withoutSenses(initialEntry), withoutSenses(updatedEntry));
    if (operations.length == 0) return;
    await lexboxApi.UpdateEntry(updatedEntry.id, operations);
  }

  async function updateSense(updatedSense: ISense) {
    const initialSense = initialEntry.senses.find(s => s.id === updatedSense.id);
    if (!initialSense) throw new Error('Sense not found in initial entry');
    let operations = jsonPatch.compare(withoutExamples(initialSense), withoutExamples(updatedSense));
    if (operations.length == 0) return;
    await lexboxApi.UpdateSense(entry.id, updatedSense.id, operations);
  }

  async function updateExample(senseId: string, updatedExample: IExampleSentence) {
    const initialSense = initialEntry.senses.find(s => s.id === senseId);
    if (!initialSense) throw new Error('Sense not found in initial entry');
    const initialExample = initialSense.exampleSentences.find(e => e.id === updatedExample.id);
    if (!initialExample) throw new Error('Example not found in initial sense');
    let operations = jsonPatch.compare(initialExample, updatedExample);
    if (operations.length == 0) return;
    await lexboxApi.UpdateExampleSentence(entry.id, senseId, updatedExample.id, operations);
  }
</script>

<div id="entry" class="editor" class:hide-empty-fields={$viewConfig.hideEmptyFields}>
  <EntryEditor on:change={e =>onChange(e.detail)} on:create={e => onCreate(e.detail)} entry={entry}/>
</div>

<style lang="postcss">
  :global(.hide-empty-fields .empty) {
    display: none !important;
  }

  .editor {
    &, & :global(:is(h2, h3)) {
      scroll-margin-top: 1rem;
    }
  }
</style>
