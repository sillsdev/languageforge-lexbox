<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import ChangeSummary from '$lib/activity/ChangeSummary.svelte';
  import type {ChangeFact} from '$lib/activity/change-summary';
  import {fwliteStoryParameters} from '../fwl-parameters';

  const {Story} = defineMeta({
    title: 'activity/summaries',
    parameters: fwliteStoryParameters({}),
  });

  const cases: {label: string; fact: ChangeFact; subject?: string; target?: string}[] = [
    {label: 'Create entry', fact: {kind: 'create', entity: 'entry', label: 'Apfel'}, subject: 'Apfel'},
    {label: 'Create sense', fact: {kind: 'create', entity: 'sense', label: 'apple'}, subject: 'Apfel › apple'},
    {label: 'Create example', fact: {kind: 'create', entity: 'example'}, subject: 'Apfel › apple'},
    {label: 'Set entry field', fact: {kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', ws: 'de', value: 'Apfelbaum'}, subject: 'Apfel'},
    {label: 'Set sense field', fact: {kind: 'setField', entity: 'sense', fieldId: 'definition', ws: 'en', value: 'a fruit tree'}, subject: 'Apfel › apple'},
    {label: 'Set field (no ws)', fact: {kind: 'setField', entity: 'entry', fieldId: 'note', value: 'a note'}, subject: 'Apfel'},
    {label: 'Clear field', fact: {kind: 'clearField', entity: 'entry', fieldId: 'citationForm', ws: 'fr'}, subject: 'Apfel'},
    {label: 'Change part of speech', fact: {kind: 'changeField', entity: 'sense', fieldId: 'partOfSpeechId'}, subject: 'Apfel › apple', target: 'Noun'},
    {label: 'Clear part of speech', fact: {kind: 'clearField', entity: 'sense', fieldId: 'partOfSpeechId'}, subject: 'Apfel › apple'},
    {label: 'Add to list', fact: {kind: 'addItem', entity: 'sense', fieldId: 'semanticDomains', label: '5.2 Food'}, subject: 'Apfel › apple'},
    {label: 'Remove from list', fact: {kind: 'removeItem', entity: 'sense', fieldId: 'semanticDomains'}, subject: 'Apfel › apple', target: '5.2 Food'},
    {label: 'Replace in list', fact: {kind: 'replaceItem', entity: 'entry', fieldId: 'publishIn', label: 'Main Dictionary'}, subject: 'Apfel'},
    {label: 'Reorder sense', fact: {kind: 'reorder', collection: 'senses'}, subject: 'Apfel', target: 'apple'},
    {label: 'Reorder example', fact: {kind: 'reorder', collection: 'examples'}, subject: 'Apfel › apple'},
    {label: 'Move sense', fact: {kind: 'moveSense'}, subject: 'Apfelbaum › apple'},
    {label: 'Link component', fact: {kind: 'componentLink', action: 'add'}, subject: 'Apple tree', target: 'Apple'},
    {label: 'Delete entry (homograph)', fact: {kind: 'delete', entity: 'entry'}, subject: 'Apfel₂'},
    {label: 'Delete sense (homograph)', fact: {kind: 'delete', entity: 'sense'}, subject: 'Apfel₂ › apple'},
    {label: 'Edit vocab field', fact: {kind: 'editObjectField', object: 'partOfSpeech', field: 'Abbreviation', ws: 'en', value: 'n'}, subject: 'Noun'},
    {label: 'Edit vocab (generic)', fact: {kind: 'editObject', object: 'partOfSpeech'}, subject: 'Noun'},
    {label: 'Create vocab', fact: {kind: 'createObject', object: 'semanticDomain', label: '5.2 Food'}, subject: '5.2 Food'},
    {label: 'Delete vocab', fact: {kind: 'deleteObject', object: 'publication'}, subject: 'School Dictionary'},
    {label: 'Bulk create (import/sync)', fact: {kind: 'bulkCreate', noun: 'semanticDomains', count: 100}},
    {label: 'Generic fallback', fact: {kind: 'generic', text: 'Create remote resource'}},
    {label: 'Create entry (no headword)', fact: {kind: 'create', entity: 'entry'}},
    {label: 'Set homograph number', fact: {kind: 'setHomograph', value: '2'}, subject: 'Apfel₂'},
    {label: 'Unlinked component', fact: {kind: 'componentLink', action: 'remove'}, subject: 'Apple tree'},
    {label: 'Set default translation', fact: {kind: 'setDefaultTranslation'}, subject: 'Apfel › apple'},
    {label: 'Long value (data pill)', fact: {kind: 'setField', entity: 'sense', fieldId: 'definition', ws: 'en', value: 'a long definition that shows how the value pill stands apart from the surrounding template words'}, subject: 'Apfel › apple'},
  ];
</script>

<Story name="All summaries">
  {#snippet template()}
    <div class="grid grid-cols-[12rem_1fr] gap-x-4 gap-y-1.5 text-sm max-w-3xl">
      {#each cases as testCase (testCase.label)}
        <div class="text-muted-foreground">{testCase.label}</div>
        <div><ChangeSummary fact={testCase.fact} subject={testCase.subject} target={testCase.target} /></div>
      {/each}
    </div>
  {/snippet}
</Story>

<!-- Mimics the narrow activity-list column: each row truncates with an ellipsis instead of wrapping/overflowing. -->
<Story name="Narrow column (truncation)">
  {#snippet template()}
    <div class="w-64 space-y-1 rounded border p-2 text-sm">
      {#each cases as testCase (testCase.label)}
        <div class="truncate"><ChangeSummary fact={testCase.fact} subject={testCase.subject} target={testCase.target} /></div>
      {/each}
    </div>
  {/snippet}
</Story>
