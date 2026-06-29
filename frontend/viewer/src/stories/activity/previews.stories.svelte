<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import ActivityItemChangePreview from '$lib/activity/ActivityItemChangePreview.svelte';
  import EntryEditor from '$lib/entry-editor/object-editors/EntryEditor.svelte';
  import {fwliteStoryParameters} from '../fwl-parameters';
  import {allWsEntry} from '$project/demo/demo-entry-data';
  import type {IChangeContext, IComplexFormComponent, IEntry, IObjectWithId} from '$lib/dotnet-types';

  const {Story} = defineMeta({
    title: 'activity/previews',
    parameters: fwliteStoryParameters({resizable: false}),
  });

  function ctx(partial: Partial<IChangeContext>): IChangeContext {
    return {commitId: 'demo', changeIndex: 0, changeName: 'Demo change', affectedEntries: [], entityType: 'Unknown', ...partial} as IChangeContext;
  }

  const entry = allWsEntry;
  const sense = entry.senses[0];
  const example = sense.exampleSentences[0];

  const entryBefore: IEntry = {...entry, lexemeForm: {...entry.lexemeForm, seh: 'nyumba'}, citationForm: {seh: 'nyumba'}};
  const entryAfter: IEntry = {...entry, lexemeForm: {...entry.lexemeForm, seh: 'nyumba zikulu'}, citationForm: {}};

  const senseBefore = {...sense, gloss: {...sense.gloss, en: 'dwelling'}};
  const senseAfter = {...sense, gloss: {...sense.gloss, en: 'house'}};

  const exampleBefore = {...example, sentence: {...example.sentence, seh: {spans: [{text: 'Nyumba ndi yaing ono', ws: 'seh'}]}}};
  const exampleAfter = example;

  const componentEntry: IEntry = {...entry, id: '00000000-0000-0000-0000-0000000000aa', lexemeForm: {seh: 'mwala'}, citationForm: {}, senses: []};
  const cfc = {
    id: '00000000-0000-0000-0000-0000000000bb',
    complexFormEntryId: entry.id,
    componentEntryId: componentEntry.id,
    order: 1,
  } as unknown as IComplexFormComponent;

  const cases: {label: string; context: IChangeContext}[] = [
    {label: 'Entry — edited', context: ctx({entityType: 'Entry', previousSnapshot: entryBefore, snapshot: entryAfter, affectedEntries: [entryAfter]})},
    {label: 'Entry — created (no before)', context: ctx({entityType: 'Entry', snapshot: entry, affectedEntries: [entry]})},
    {label: 'Sense — edited', context: ctx({entityType: 'Sense', previousSnapshot: senseBefore, snapshot: senseAfter, affectedEntries: [entry]})},
    {label: 'Example — edited', context: ctx({entityType: 'ExampleSentence', previousSnapshot: exampleBefore, snapshot: exampleAfter, affectedEntries: [entry]})},
    {label: 'Complex form component', context: ctx({entityType: 'ComplexFormComponent', snapshot: cfc, affectedEntries: [entry, componentEntry]})},
    {label: 'Remote resource (generic JSON)', context: ctx({entityType: 'RemoteResource', snapshot: {id: 'res-1', fileName: 'audio.mp3', uploaded: true} as unknown as IObjectWithId})},
    {label: 'No preview available', context: ctx({entityType: 'Unknown'})},
  ];
</script>

<Story name="Change previews">
  {#snippet template()}
    <div class="space-y-6 max-w-3xl p-2">
      {#each cases as testCase (testCase.label)}
        <div>
          <div class="font-bold text-sm mb-1 text-muted-foreground">{testCase.label}</div>
          <div class="border rounded p-3">
            <ActivityItemChangePreview context={testCase.context} />
          </div>
        </div>
      {/each}
    </div>
  {/snippet}
</Story>

<Story name="Created entry (collapsed)">
  {#snippet template()}
    <div class="max-w-3xl border rounded p-3">
      <EntryEditor entry={structuredClone(allWsEntry)} readonly modalMode canAddSense={false} canAddExample={false} />
    </div>
  {/snippet}
</Story>
