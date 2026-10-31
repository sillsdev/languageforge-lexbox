<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import {Router} from 'svelte-routing';
  import type {
    IActivityChangeInfo,
    IChangeContext,
    IChangeEntity,
    IComplexFormComponent,
    ICustomView,
    IEntry,
    IExampleSentence,
    IObjectWithId,
    ISemanticDomain,
    ISense,
    IWritingSystem,
  } from '$lib/dotnet-types';
  import {ViewBase} from '$lib/dotnet-types';
  import {_entries, partsOfSpeech, writingSystems} from '$project/demo/demo-entry-data';
  import ActivityItemChangePreview from '$lib/activity/ActivityItemChangePreview.svelte';

  const {Story} = defineMeta({
    title: 'activity/change preview',
    parameters: {fwlite: {resizable: false}},
  });

  type Example = {
    title: string;
    changeName: string;
    entityType: string;
    before?: IObjectWithId;
    after?: IObjectWithId;
    /** The current entries, after every later change */
    affected?: IEntry[];
    $type?: string;
    info?: IActivityChangeInfo;
  };

  const entry: IEntry = _entries[0];
  const renamed: IEntry = {...entry, citationForm: {}, lexemeForm: {...entry.lexemeForm, seh: 'nyumbo'}};
  const renamedSince: IEntry = {...entry, citationForm: {}, lexemeForm: {...entry.lexemeForm, seh: 'nyumba yaikulu'}, homographNumber: 2};
  const deleted: IEntry = {...entry, deletedAt: '2026-10-02T10:00:00Z'};
  const other: IEntry = _entries[1];
  const longHeadword: IEntry = {...entry, id: 'long', citationForm: {}, lexemeForm: {seh: 'nyumba-ya-kale-kwambiri-yomwe-inamangidwa-ndi-agogo-athu'}};

  const sense: ISense = entry.senses[0];
  const senseAfter: ISense = {...sense, gloss: {...sense.gloss, en: 'home'}, partOfSpeech: partsOfSpeech[0], partOfSpeechId: partsOfSpeech[0].id};
  const movedSense: ISense = {...sense, order: 3} as ISense;

  const example: IExampleSentence = sense.exampleSentences[0];
  const exampleAfter: IExampleSentence = {
    ...example,
    sentence: {...example.sentence, seh: {spans: [{text: 'Nyumba yanga ndi ikulu kwambiri', ws: 'seh'}]}},
  };

  const link: IComplexFormComponent = {
    id: 'cfc',
    complexFormEntryId: longHeadword.id,
    complexFormHeadword: 'nyumba-ya-kale',
    componentEntryId: other.id,
    componentHeadword: 'dance',
  };

  const posBefore = partsOfSpeech[1];
  const posAfter = {...posBefore, name: {...posBefore.name, en: 'Common noun'}};
  const domain: ISemanticDomain = {id: 'sd', code: '1.1', name: {en: 'Sky'}, abbreviation: {}, description: {}, predefined: false};
  const wsBefore: IWritingSystem = writingSystems.vernacular[0];
  const wsAfter: IWritingSystem = {...wsBefore, abbreviation: 'Sen', font: 'Charis SIL Compact'};
  const view: ICustomView = {id: 'view', name: 'Quick gloss', base: ViewBase.FwLite, entryFields: [{fieldId: 'lexemeForm'}], senseFields: [{fieldId: 'gloss'}], exampleFields: []};

  const examples: Example[] = [
    {title: 'Entry edit, headword changed since', changeName: 'Edit entry', entityType: 'Entry', before: entry, after: renamed, affected: [renamedSince]},
    {title: 'Entry edit, current headword', changeName: 'Edit entry', entityType: 'Entry', before: entry, after: renamed, affected: [renamed]},
    {title: 'Entry edit, deleted since', changeName: 'Edit entry', entityType: 'Entry', before: entry, after: renamed, affected: [deleted]},
    {title: 'Entry create', changeName: 'Create entry', entityType: 'Entry', after: entry, affected: [entry]},
    {title: 'Entry delete', changeName: 'Delete entry', entityType: 'Entry', before: entry, after: deleted, affected: [deleted]},
    {title: 'Sense edit', changeName: 'Edit sense', entityType: 'Sense', before: sense, after: senseAfter, affected: [entry],
      info: {subject: 'nyumba › home₁', owningEntryHeadword: 'nyumba'}},
    {title: 'Sense create, long headword', changeName: 'Create sense', entityType: 'Sense', after: sense, affected: [longHeadword],
      info: {subject: longHeadword.lexemeForm.seh, owningEntryHeadword: longHeadword.lexemeForm.seh}},
    {title: 'Sense reorder', changeName: 'Reorder sense', entityType: 'Sense', before: sense, after: movedSense, affected: [entry],
      $type: 'SetOrderChange:Sense', info: {subject: 'nyumba', owningEntryHeadword: 'nyumba', target: 'house₁'}},
    {title: 'Example edit', changeName: 'Edit example sentence', entityType: 'ExampleSentence', before: example, after: exampleAfter, affected: [entry],
      info: {subject: 'nyumba › house₁', owningEntryHeadword: 'nyumba', target: 'Nyumba yanga ndi...'}},
    {title: 'Example edit, unresolved subject', changeName: 'Edit example sentence', entityType: 'ExampleSentence', before: example, after: exampleAfter, affected: [entry]},
    {title: 'Example reorder', changeName: 'Reorder example sentence', entityType: 'ExampleSentence', before: example, after: {...example, order: 2} as IExampleSentence,
      affected: [entry], $type: 'SetOrderChange:ExampleSentence', info: {subject: 'nyumba › house₁', owningEntryHeadword: 'nyumba'}},
    {title: 'Component added', changeName: 'Add component', entityType: 'ComplexFormComponent', after: link, affected: [longHeadword, other],
      info: {subject: 'nyumba-ya-kale', owningEntryHeadword: 'nyumba-ya-kale'}},
    {title: 'Component removed', changeName: 'Delete component', entityType: 'ComplexFormComponent', before: link, after: {...link, deletedAt: '2026-10-02T10:00:00Z'},
      affected: [longHeadword, other]},
    {title: 'Component reorder', changeName: 'Reorder component', entityType: 'ComplexFormComponent', before: link, after: {...link, order: 2} as IComplexFormComponent,
      affected: [longHeadword, other], $type: 'SetOrderChange:ComplexFormComponent'},
    {title: 'Part of speech edit', changeName: 'Edit part of speech', entityType: 'PartOfSpeech', before: posBefore, after: posAfter},
    {title: 'Semantic domain create', changeName: 'Create semantic domain', entityType: 'SemanticDomain', after: domain},
    {title: 'Writing system edit', changeName: 'Edit writing system', entityType: 'WritingSystem', before: wsBefore, after: wsAfter},
    {title: 'Writing system reorder', changeName: 'Reorder writing system', entityType: 'WritingSystem', before: wsBefore, after: {...wsBefore},
      $type: 'SetOrderChange:WritingSystem'},
    {title: 'Custom view delete', changeName: 'Delete custom view', entityType: 'CustomView', before: view, after: {...view, deletedAt: '2026-10-02T10:00:00Z'}},
    {title: 'Unknown change', changeName: 'Upload file', entityType: 'Unknown'},
  ];

  function context(example: Example): IChangeContext {
    return {
      commitId: 'commit',
      changeIndex: 0,
      changeName: example.changeName,
      entityType: example.entityType,
      previousSnapshot: example.before,
      snapshot: example.after,
      affectedEntries: example.affected ?? [],
    };
  }

  function change(example: Example): IChangeEntity {
    return {
      index: 0,
      commitId: 'commit',
      entityId: (example.after ?? example.before)?.id ?? '',
      authoredModelVersion: 1,
      change: {entityId: '', ...(example.$type ? {$type: example.$type} : {})},
    };
  }
</script>

<!-- Every kind of change the activity view previews, each under its own header -->
<Story name="Every kind of change">
  {#snippet template()}
    <Router>
      <div class="flex flex-col gap-4 max-w-3xl">
        {#each examples as example (example.title)}
          <section class="border rounded p-4" data-testid="story-change">
            <div class="text-xs uppercase text-muted-foreground mb-2">{example.title}</div>
            <ActivityItemChangePreview context={context(example)} change={change(example)} info={example.info} />
          </section>
        {/each}
      </div>
    </Router>
  {/snippet}
</Story>
