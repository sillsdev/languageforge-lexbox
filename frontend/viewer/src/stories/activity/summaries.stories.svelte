<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import ChangeSummary from '$lib/activity/ChangeSummary.svelte';
  import type {ChangeFact} from '$lib/activity/change-summary';
  import type {IActivityChangeInfo} from '$lib/dotnet-types/generated-types/LcmCrdt/IActivityChangeInfo';
  import {fwliteStoryParameters} from '../fwl-parameters';

  const {Story} = defineMeta({
    title: 'activity/summaries',
    parameters: fwliteStoryParameters({}),
  });

  const cases: {label: string; fact: ChangeFact; info?: IActivityChangeInfo}[] = [
    {label: 'Create entry', fact: {kind: 'create', entity: 'entry', label: 'Apfel'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}}},
    {label: 'Create sense', fact: {kind: 'create', entity: 'sense', label: 'apple'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, target: {text: 'apple', homographNumber: 0}}},
    {label: 'Create example', fact: {kind: 'create', entity: 'example'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, sense: {label: 'apple'}}},
    {label: 'Set entry field', fact: {kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', ws: 'de', value: 'Apfelbaum'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}}},
    {label: 'Set sense field', fact: {kind: 'setField', entity: 'sense', fieldId: 'definition', ws: 'en', value: 'a fruit tree'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, sense: {label: 'apple'}}},
    {label: 'Set field (no ws)', fact: {kind: 'setField', entity: 'entry', fieldId: 'note', value: 'a note'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}}},
    {label: 'Clear field', fact: {kind: 'clearField', entity: 'entry', fieldId: 'citationForm', ws: 'fr'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}}},
    {label: 'Change part of speech', fact: {kind: 'changeField', entity: 'sense', fieldId: 'partOfSpeechId'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, sense: {label: 'apple'}, target: {text: 'Noun', homographNumber: 0}}},
    {label: 'Clear part of speech', fact: {kind: 'clearField', entity: 'sense', fieldId: 'partOfSpeechId'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, sense: {label: 'apple'}}},
    {label: 'Add to list', fact: {kind: 'addItem', entity: 'sense', fieldId: 'semanticDomains', label: '5.2 Food'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, sense: {label: 'apple'}}},
    {label: 'Remove from list', fact: {kind: 'removeItem', entity: 'sense', fieldId: 'semanticDomains'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, sense: {label: 'apple'}, target: {text: '5.2 Food', homographNumber: 0}}},
    {label: 'Replace in list', fact: {kind: 'replaceItem', entity: 'entry', fieldId: 'publishIn', label: 'Main Dictionary'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}}},
    {label: 'Reorder sense', fact: {kind: 'reorder', collection: 'senses'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, target: {text: 'apple', homographNumber: 0}}},
    {label: 'Reorder example', fact: {kind: 'reorder', collection: 'examples'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, sense: {label: 'apple'}}},
    {label: 'Move sense', fact: {kind: 'moveSense'}, info: {entry: {id: 'Apfelbaum', headword: 'Apfelbaum', homographNumber: 0}, sense: {label: 'apple'}}},
    {label: 'Move example', fact: {kind: 'moveExample'}, info: {entry: {id: 'Apfelbaum', headword: 'Apfelbaum', homographNumber: 0}, sense: {label: 'apple'}}},
    {label: 'Link component', fact: {kind: 'componentLink', action: 'add'}, info: {entry: {id: 'Apple tree', headword: 'Apple tree', homographNumber: 0}, target: {text: 'Apple', homographNumber: 0}}},
    {label: 'Delete entry (homograph)', fact: {kind: 'delete', entity: 'entry'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 2}}},
    {label: 'Delete sense (homograph)', fact: {kind: 'delete', entity: 'sense'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 2}, target: {text: 'apple', homographNumber: 0}}},
    {label: 'Delete example', fact: {kind: 'delete', entity: 'example'}, info: {entry: {id: 'Bank', headword: 'Bank', homographNumber: 2}, sense: {label: 'shore', number: 1}, target: {text: 'We sat on the bank.', homographNumber: 0}}},
    {label: 'Delete example (sense deleted too)', fact: {kind: 'delete', entity: 'example'}, info: {entry: {id: 'Bank', headword: 'Bank', homographNumber: 2}, sense: {label: 'shore'}, target: {text: 'We sat on the bank.', homographNumber: 0}}},
    {label: 'Edit vocab field', fact: {kind: 'editObjectField', object: 'partOfSpeech', field: 'Abbreviation', ws: 'en', value: 'n'}, info: {objectName: 'Noun'}},
    {label: 'Edit vocab (generic)', fact: {kind: 'editObject', object: 'partOfSpeech'}, info: {objectName: 'Noun'}},
    {label: 'Create vocab', fact: {kind: 'createObject', object: 'semanticDomain', label: '5.2 Food'}, info: {objectName: '5.2 Food'}},
    {label: 'Delete vocab', fact: {kind: 'deleteObject', object: 'publication'}, info: {objectName: 'School Dictionary'}},
    {label: 'Bulk create (import/sync)', fact: {kind: 'bulkCreate', noun: 'semanticDomains', count: 100}},
    {label: 'Generic fallback', fact: {kind: 'generic', text: 'Create remote resource'}},
    {label: 'Create entry (no headword)', fact: {kind: 'create', entity: 'entry'}},
    {label: 'Set homograph number', fact: {kind: 'setHomograph', value: '2'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 2}}},
    {label: 'Unlinked component', fact: {kind: 'componentLink', action: 'remove'}, info: {entry: {id: 'Apple tree', headword: 'Apple tree', homographNumber: 0}}},
    {label: 'Edit numbered sense', fact: {kind: 'setField', entity: 'sense', fieldId: 'definition', ws: 'en', value: 'land along a river'}, info: {entry: {id: 'Bank', headword: 'Bank', homographNumber: 2}, sense: {label: 'shore', number: 1}}},
    {label: 'Edit unnamed sense', fact: {kind: 'setField', entity: 'sense', fieldId: 'definition', ws: 'en', value: 'a fruit'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 2}, sense: {}}},
    {label: 'Set default translation', fact: {kind: 'setDefaultTranslation'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, sense: {label: 'apple'}}},
    {label: 'Long value (data pill)', fact: {kind: 'setField', entity: 'sense', fieldId: 'definition', ws: 'en', value: 'a long definition that shows how the value pill stands apart from the surrounding template words'}, info: {entry: {id: 'Apfel', headword: 'Apfel', homographNumber: 0}, sense: {label: 'apple'}}},
  ];
</script>

<Story name="All summaries">
  {#snippet template()}
    <div class="grid grid-cols-[12rem_1fr] gap-x-4 gap-y-1.5 text-sm max-w-3xl">
      {#each cases as testCase (testCase.label)}
        <div class="text-muted-foreground">{testCase.label}</div>
        <div><ChangeSummary fact={testCase.fact} info={testCase.info} /></div>
      {/each}
    </div>
  {/snippet}
</Story>

<!-- Mimics the narrow activity-list column: each row truncates with an ellipsis instead of wrapping/overflowing. -->
<Story name="Narrow column (truncation)">
  {#snippet template()}
    <div class="w-64 space-y-1 rounded border p-2 text-sm">
      {#each cases as testCase (testCase.label)}
        <div class="truncate"><ChangeSummary fact={testCase.fact} info={testCase.info} /></div>
      {/each}
    </div>
  {/snippet}
</Story>
