<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import * as Editor from '$lib/components/editor';
  import {MorphTypeKind, ViewBase, type ICustomView, type IEntry, type IExampleSentence, type IPartOfSpeech, type ISense, type IWritingSystem} from '$lib/dotnet-types';
  import {_entries, partsOfSpeech, writingSystems} from '$project/demo/demo-entry-data';
  import {RichTextToggle} from '$lib/dotnet-types/generated-types/MiniLcm/Models/RichTextToggle';
  import DiffEntryPrimitive from '$lib/entry-editor/diff-view/DiffEntryPrimitive.svelte';
  import DiffSensePrimitive from '$lib/entry-editor/diff-view/DiffSensePrimitive.svelte';
  import DiffExamplePrimitive from '$lib/entry-editor/diff-view/DiffExamplePrimitive.svelte';
  import DiffFieldRows from '$lib/entry-editor/diff-view/DiffFieldRows.svelte';
  import DiffEntryLinkList from '$lib/entry-editor/diff-view/DiffEntryLinkList.svelte';
  import {objectDiffRows} from '$lib/entry-editor/diff-view/diff-fields';

  const {Story} = defineMeta({
    title: 'editor/diff/entities',
  });

  const entryBefore: IEntry = _entries[0];
  const entryAfter: IEntry = {
    ...entryBefore,
    lexemeForm: {...entryBefore.lexemeForm, seh: 'nyumbo'},
    morphType: MorphTypeKind.BoundStem,
  };

  const senseBefore: ISense = _entries[0].senses[0];
  const senseAfter: ISense = {
    ...senseBefore,
    gloss: {...senseBefore.gloss, en: 'home'},
    partOfSpeech: partsOfSpeech[0],
    partOfSpeechId: partsOfSpeech[0].id,
  };

  const exampleBefore: IExampleSentence = senseBefore.exampleSentences[0];
  const exampleAfter: IExampleSentence = {
    ...exampleBefore,
    sentence: {...exampleBefore.sentence, seh: {spans: [{text: 'Nyumba yanga ndi ikulu kwambiri', ws: 'seh'}]}},
  };
  // Bold only: the diff compares plain text
  const exampleFormatted: IExampleSentence = {
    ...exampleBefore,
    sentence: {...exampleBefore.sentence, seh: {spans: [{text: 'Nyumba yanga ndi ikulu', ws: 'seh', bold: RichTextToggle.On}]}},
  };
  const homographOnly: IEntry = {...entryBefore, homographNumber: 1};
  const newSense: ISense = {...senseBefore, id: 'new', definition: {}, gloss: {en: 'shelter'}, semanticDomains: [], exampleSentences: []};

  const posBefore: IPartOfSpeech = partsOfSpeech[1];
  const posAfter: IPartOfSpeech = {...posBefore, name: {...posBefore.name, en: 'Common noun'}};

  const wsBefore: IWritingSystem = writingSystems.vernacular[0];
  const wsAfter: IWritingSystem = {...wsBefore, abbreviation: 'Sen', icuCollationRules: '&a < b'};

  function link(id: string, headword: string) {
    return {id, headword};
  }
  const componentsBefore = [link('1', 'nyumba'), link('2', 'ya'), link('3', 'kale')];
  const componentsAfter = [link('1', 'nyumba'), link('3', 'kale'), link('4', 'kulu')];

  const viewBefore: ICustomView = {
    id: 'view',
    name: 'Quick gloss',
    base: ViewBase.FwLite,
    entryFields: [{fieldId: 'lexemeForm'}, {fieldId: 'citationForm'}],
    senseFields: [{fieldId: 'gloss'}],
    exampleFields: [],
  };
  const viewAfter: ICustomView = {...viewBefore, entryFields: [{fieldId: 'lexemeForm'}], senseFields: [{fieldId: 'gloss'}, {fieldId: 'definition'}], analysis: []};
</script>

<Story name="Entities and objects">
  {#snippet template()}
    <Editor.Root>
      <Editor.Grid>
        <div class="col-span-full font-bold">Entry (morph type isn't an editor field)</div>
        <DiffEntryPrimitive before={entryBefore} after={entryAfter} />

        <div class="col-span-full font-bold">Components (links between entries)</div>
        <Editor.Field.Root>
          <Editor.Field.Title name="Components" />
          <Editor.Field.Body>
            <DiffEntryLinkList before={componentsBefore} after={componentsAfter}
                               getKey={(l) => l.id} getHeadword={(l) => l.headword} touched={(l) => l.id === '3'} />
          </Editor.Field.Body>
        </Editor.Field.Root>

        <div class="col-span-full font-bold">Sense</div>
        <DiffSensePrimitive before={senseBefore} after={senseAfter} />

        <div class="col-span-full font-bold">Example</div>
        <DiffExamplePrimitive before={exampleBefore} after={exampleAfter} />

        <div class="col-span-full font-bold">Part of speech</div>
        <DiffFieldRows rows={objectDiffRows.PartOfSpeech} before={posBefore} after={posAfter} />

        <div class="col-span-full font-bold">Writing system</div>
        <DiffFieldRows rows={objectDiffRows.WritingSystem} before={wsBefore} after={wsAfter} />

        <div class="col-span-full font-bold">Custom view</div>
        <DiffFieldRows rows={objectDiffRows.CustomView} before={viewBefore} after={viewAfter} />
      </Editor.Grid>
    </Editor.Root>
  {/snippet}
</Story>

<!-- What the activity view shows: each diff keeps its own "Show unchanged fields" state. -->
<Story name="Changes only">
  {#snippet template()}
    <Editor.Root>
      <Editor.Grid>
        <div class="col-span-full font-bold">Entry: lexeme form and morph type</div>
        <DiffEntryPrimitive mode="changes" before={entryBefore} after={entryAfter} />

        <div class="col-span-full font-bold">Entry: homograph number only</div>
        <DiffEntryPrimitive mode="changes" before={entryBefore} after={homographOnly} />

        <div class="col-span-full font-bold">Sense</div>
        <DiffSensePrimitive mode="changes" before={senseBefore} after={senseAfter} />

        <div class="col-span-full font-bold">New sense</div>
        <DiffSensePrimitive mode="changes" after={newSense} />

        <div class="col-span-full font-bold">Example</div>
        <DiffExamplePrimitive mode="changes" before={exampleBefore} after={exampleAfter} />

        <div class="col-span-full font-bold">Example: formatting only</div>
        <DiffExamplePrimitive mode="changes" before={exampleBefore} after={exampleFormatted} />
      </Editor.Grid>
    </Editor.Root>
  {/snippet}
</Story>
