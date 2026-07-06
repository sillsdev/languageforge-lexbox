/* eslint-disable @typescript-eslint/naming-convention -- change payloads below mirror the PascalCase CRDT wire format on purpose */
import {describe, it, expect} from 'vitest';
import {commitBadge, describeActivity, describeActivityCapped, describeChange, explicitlyHandledChangeTypes, factCategory, groupByRootEntry, isHandledChangeType, recognizeBulkCreate, recognizeTreeCommit, ROW_FACT_CAP, summarizeActivity, type ChangeFact, type ChangeFactWithSubject} from './change-summary';
import {knownChangeTypes} from '$lib/dotnet-types/generated-types/LcmCrdt/ChangeTypes';
import type {IChangeEntity} from '$lib/dotnet-types';

function changeEntity(change: object): IChangeEntity {
  return {index: 0, commitId: 'c', entityId: 'e', change} as unknown as IChangeEntity;
}

describe('describeChange', () => {
  it('describes a jsonPatch entry edit (PascalCase wire format)', () => {
    const facts = describeChange(changeEntity({
      '$type': 'jsonPatch:Entry',
      EntityId: 'e',
      PatchDocument: [{op: 'replace', path: '/LexemeForm/en', value: 'asdasd'}],
    }));
    expect(facts).toEqual([{kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', ws: 'en', value: 'asdasd'}]);
  });

  it('summarizes a homograph-number patch specifically (not a generic patch)', () => {
    const facts = describeChange(changeEntity({
      '$type': 'jsonPatch:Entry',
      PatchDocument: [{op: 'replace', path: '/HomographNumber', value: 2}],
    }));
    expect(facts).toEqual([{kind: 'setHomograph', value: '2'}]);
  });

  it('tolerates camelCase wire format', () => {
    const facts = describeChange(changeEntity({
      '$type': 'jsonPatch:Sense',
      entityId: 'e',
      patchDocument: [{op: 'replace', path: '/gloss/en', value: 'walk'}],
    }));
    expect(facts).toEqual([{kind: 'setField', entity: 'sense', fieldId: 'gloss', ws: 'en', value: 'walk'}]);
  });

  it('treats a remove op as clearing the field', () => {
    const facts = describeChange(changeEntity({
      '$type': 'jsonPatch:Entry',
      PatchDocument: [{op: 'remove', path: '/CitationForm/fr'}],
    }));
    expect(facts).toEqual([{kind: 'clearField', entity: 'entry', fieldId: 'citationForm', ws: 'fr'}]);
  });

  it('extracts plain text from a rich-string value', () => {
    const facts = describeChange(changeEntity({
      '$type': 'jsonPatch:Sense',
      PatchDocument: [{op: 'replace', path: '/Definition/en', value: {Spans: [{Text: 'to ', Ws: 'en'}, {Text: 'walk', Ws: 'en'}]}}],
    }));
    expect(facts).toEqual([{kind: 'setField', entity: 'sense', fieldId: 'definition', ws: 'en', value: 'to walk'}]);
  });

  it('flags an audio-WS set as audio instead of leaking the media URI', () => {
    const facts = describeChange(changeEntity({
      '$type': 'jsonPatch:ExampleSentence',
      PatchDocument: [{op: 'add', path: '/Sentence/seh-Zxxx-x-audio', value: {Spans: [{Text: 'sil-media://lexbox.org/abc', Ws: 'seh-Zxxx-x-audio'}]}}],
    }));
    expect(facts).toEqual([{kind: 'setField', entity: 'example', fieldId: 'sentence', value: '', audio: true}]);
  });

  it('flags a sil-media value as audio even on a non-audio ws', () => {
    const facts = describeChange(changeEntity({
      '$type': 'jsonPatch:ExampleSentence',
      PatchDocument: [{op: 'replace', path: '/Sentence/seh', value: 'sil-media://lexbox.org/xyz'}],
    }));
    expect(facts).toEqual([{kind: 'setField', entity: 'example', fieldId: 'sentence', value: '', audio: true}]);
  });

  it('clears an audio ws without showing the raw audio ws code', () => {
    const facts = describeChange(changeEntity({
      '$type': 'jsonPatch:ExampleSentence',
      PatchDocument: [{op: 'remove', path: '/Sentence/seh-Zxxx-x-audio'}],
    }));
    expect(facts).toEqual([{kind: 'clearField', entity: 'example', fieldId: 'sentence', audio: true}]);
  });

  it('describes entry creation with the headword', () => {
    const facts = describeChange(changeEntity({'$type': 'CreateEntryChange', LexemeForm: {en: 'apple'}, EntityId: 'e'}));
    expect(facts).toEqual([{kind: 'create', entity: 'entry', label: 'apple'}]);
  });

  it('describes sense creation with the gloss', () => {
    const facts = describeChange(changeEntity({'$type': 'CreateSenseChange', Gloss: {en: 'fruit'}, EntityId: 'e'}));
    expect(facts).toEqual([{kind: 'create', entity: 'sense', label: 'fruit'}]);
  });

  it('describes an example created with sentence text', () => {
    const facts = describeChange(changeEntity({'$type': 'CreateExampleSentenceChange', Sentence: {seh: 'Nyumba yanga'}, EntityId: 'e'}));
    expect(facts).toEqual([{kind: 'create', entity: 'example', label: 'Nyumba yanga', audioOnly: false}]);
  });

  it('flags an audio-only example instead of leaking the media URI', () => {
    const facts = describeChange(changeEntity({
      '$type': 'CreateExampleSentenceChange',
      Sentence: {'seh-Zxxx-x-audio': {Spans: [{Text: 'sil-media://lexbox.org/abc', Ws: 'seh-Zxxx-x-audio'}]}},
      EntityId: 'e',
    }));
    expect(facts).toEqual([{kind: 'create', entity: 'example', label: undefined, audioOnly: true}]);
  });

  it('describes deletion', () => {
    expect(describeChange(changeEntity({'$type': 'delete:Entry', EntityId: 'e'})))
      .toEqual([{kind: 'delete', entity: 'entry'}]);
  });

  it('describes a media-resource create/upload/delete (assumed audio — the only resource kind today)', () => {
    expect(describeChange(changeEntity({'$type': 'create:remote-resource', EntityId: 'r1'})))
      .toEqual([{kind: 'mediaResource', action: 'add', resourceId: 'r1', audio: true}]);
    expect(describeChange(changeEntity({'$type': 'uploaded:RemoteResource', EntityId: 'r1'})))
      .toEqual([{kind: 'mediaResource', action: 'upload', resourceId: 'r1', audio: true}]);
    expect(describeChange(changeEntity({'$type': 'delete:RemoteResource', EntityId: 'r1'})))
      .toEqual([{kind: 'mediaResource', action: 'delete', resourceId: 'r1', audio: true}]);
  });

  it('falls back to a PAST-TENSE humanized type for unrecognized changes', () => {
    expect(describeChange(changeEntity({'$type': 'SomeFutureChange', EntityId: 'e'})))
      .toEqual([{kind: 'generic', text: 'Some future'}]);
    // Leading verb is past-tensed so the fallback matches every other summary's tense.
    expect(describeChange(changeEntity({'$type': 'create:widget', EntityId: 'e'})))
      .toEqual([{kind: 'generic', text: 'Created widget'}]);
  });

  it('yields one fact per patch operation', () => {
    const facts = describeChange(changeEntity({
      '$type': 'jsonPatch:Entry',
      PatchDocument: [
        {op: 'replace', path: '/LexemeForm/en', value: 'a'},
        {op: 'replace', path: '/CitationForm/en', value: 'b'},
      ],
    }));
    expect(facts).toHaveLength(2);
  });

  it('describes adding a semantic domain with its code and name', () => {
    expect(describeChange(changeEntity({'$type': 'AddSemanticDomainChange', SemanticDomain: {Id: 'd', Code: '1.1', Name: {en: 'Sky'}}, EntityId: 'e'})))
      .toEqual([{kind: 'addItem', entity: 'sense', fieldId: 'semanticDomains', label: '1.1 Sky'}]);
  });

  it('describes adding a complex form type with its name', () => {
    expect(describeChange(changeEntity({'$type': 'AddComplexFormTypeChange', ComplexFormType: {Id: 'c', Name: {en: 'Compound'}}, EntityId: 'e'})))
      .toEqual([{kind: 'addItem', entity: 'entry', fieldId: 'complexFormTypes', label: 'Compound'}]);
  });

  it('describes removing a publication without a label (id only)', () => {
    expect(describeChange(changeEntity({'$type': 'RemovePublicationChange', PublicationId: 'p', EntityId: 'e'})))
      .toEqual([{kind: 'removeItem', entity: 'entry', fieldId: 'publishIn'}]);
  });

  it('routes set-part-of-speech to a field change (named later from the resolved target)', () => {
    expect(describeChange(changeEntity({'$type': 'SetPartOfSpeechChange', PartOfSpeechId: 'p', EntityId: 'e'})))
      .toEqual([{kind: 'changeField', entity: 'sense', fieldId: 'partOfSpeechId'}]);
  });

  it('treats a null part-of-speech id as clearing the field', () => {
    expect(describeChange(changeEntity({'$type': 'SetPartOfSpeechChange', PartOfSpeechId: null, EntityId: 'e'})))
      .toEqual([{kind: 'clearField', entity: 'sense', fieldId: 'partOfSpeechId'}]);
  });

  it('decodes a jsonPatch on a vocab object into a per-field edit', () => {
    expect(describeChange(changeEntity({
      '$type': 'jsonPatch:PartOfSpeech',
      PatchDocument: [{op: 'replace', path: '/Name/en', value: 'Verb'}],
    }))).toEqual([{kind: 'editObjectField', object: 'partOfSpeech', field: 'Name', ws: 'en', value: 'Verb'}]);
  });

  it('falls back to a generic object edit when a vocab jsonPatch has no decodable ops', () => {
    expect(describeChange(changeEntity({'$type': 'jsonPatch:PartOfSpeech', PatchDocument: []})))
      .toEqual([{kind: 'editObject', object: 'partOfSpeech'}]);
  });

  it('describes reordering by collection', () => {
    expect(describeChange(changeEntity({'$type': 'SetOrderChange:Sense', Order: 1, EntityId: 'e'})))
      .toEqual([{kind: 'reorder', collection: 'senses'}]);
  });

  it('describes the sense-picture and main-publication changes (added on develop)', () => {
    expect(describeChange(changeEntity({'$type': 'CreateSensePictureChange', EntityId: 'e'}))).toEqual([{kind: 'sensePicture', action: 'add'}]);
    expect(describeChange(changeEntity({'$type': 'RemoveSensePictureChange', EntityId: 'e'}))).toEqual([{kind: 'sensePicture', action: 'remove'}]);
    expect(describeChange(changeEntity({'$type': 'UpdateSensePictureChange', EntityId: 'e'}))).toEqual([{kind: 'sensePicture', action: 'update'}]);
    expect(describeChange(changeEntity({'$type': 'ReorderSensePictureChange', EntityId: 'e'}))).toEqual([{kind: 'sensePicture', action: 'reorder'}]);
    expect(describeChange(changeEntity({'$type': 'SetMainPublicationChange', EntityId: 'e'}))).toEqual([{kind: 'setMainPublication'}]);
  });

  it('describes moving a sense', () => {
    expect(describeChange(changeEntity({'$type': 'MoveSenseToEntryChange', EntryId: 'x', EntityId: 'e'})))
      .toEqual([{kind: 'moveSense'}]);
  });

  it('describes creating a vocabulary object with its name', () => {
    expect(describeChange(changeEntity({'$type': 'CreatePartOfSpeechChange', Name: {en: 'Noun'}, EntityId: 'e'})))
      .toEqual([{kind: 'createObject', object: 'partOfSpeech', label: 'Noun'}]);
  });

  it('labels a created semantic domain with its code and name (not just the code)', () => {
    expect(describeChange(changeEntity({'$type': 'CreateSemanticDomainChange', Code: '5.2', Name: {en: 'Food'}, EntityId: 'd'})))
      .toEqual([{kind: 'createObject', object: 'semanticDomain', label: '5.2 Food'}]);
  });

  it('labels an edited custom view with its name, like the create does', () => {
    expect(describeChange(changeEntity({'$type': 'EditCustomViewChange', Name: 'Example sentences (pt)', EntityId: 'v'})))
      .toEqual([{kind: 'editObject', object: 'customView', label: 'Example sentences (pt)'}]);
  });

  // Substrate plumbing that intentionally uses the generic humanized fallback (not a user-facing edit).
  const INTENTIONALLY_GENERIC = new Set<string>([
    'create:pendingUpload',
  ]);

  it('handles every backend change type (generated list) or allows it as intentionally generic', () => {
    const unhandled = knownChangeTypes.filter((type) => !isHandledChangeType(type) && !INTENTIONALLY_GENERIC.has(type));
    expect(unhandled).toEqual([]);
  });

  it('has no handler for a change type the backend never emits (reverse coverage — no dead handlers)', () => {
    const known = new Set<string>(knownChangeTypes);
    const stale = explicitlyHandledChangeTypes.filter((type) => !known.has(type));
    expect(stale).toEqual([]);
  });
});

describe('describeActivity', () => {
  it('threads the resolved subject and target onto each fact of a change', () => {
    const result = describeActivity(
      [changeEntity({'$type': 'RemoveSemanticDomainChange', SemanticDomainId: 'd', EntityId: 'e'})],
      [{subject: 'Apfel › apple', rootEntryId: 'e1', target: '5.2 Food'}],
    );
    expect(result).toEqual([{
      fact: {kind: 'removeItem', entity: 'sense', fieldId: 'semanticDomains'},
      subject: 'Apfel › apple',
      rootEntryId: 'e1',
      target: '5.2 Food',
    }]);
  });

  it('threads a media-resource fact through unchanged (audio assumed at describe time)', () => {
    const result = describeActivity([changeEntity({'$type': 'create:remote-resource', EntityId: 'r1'})]);
    expect(result[0].fact).toEqual({kind: 'mediaResource', action: 'add', resourceId: 'r1', audio: true});
  });
});

describe('recognizeTreeCommit', () => {
  it('recognizes an entry-creation tree (create + its tree) as "Created entry X"', () => {
    const result = recognizeTreeCommit(
      [changeEntity({'$type': 'CreateEntryChange'}), changeEntity({'$type': 'CreateSenseChange'})],
      [{subject: 'Apfel', rootEntryId: 'e1'}, {subject: 'Apfel › apple', rootEntryId: 'e1'}],
      ['CreateEntryChange', 'CreateSenseChange'],
    );
    expect(result?.fact).toMatchObject({kind: 'create', entity: 'entry'});
    expect(result?.subject).toBe('Apfel');
  });

  it('recognizes a sense-creation tree (sense + its examples) as "Added sense X"', () => {
    const result = recognizeTreeCommit(
      [changeEntity({'$type': 'CreateSenseChange'}), changeEntity({'$type': 'CreateExampleSentenceChange'})],
      [{subject: 'Apfel › apple', rootEntryId: 'e1'}, {subject: 'Apfel › apple', rootEntryId: 'e1'}],
      ['CreateSenseChange', 'CreateExampleSentenceChange'],
    );
    expect(result?.fact).toMatchObject({kind: 'create', entity: 'sense'});
    expect(result?.subject).toBe('Apfel › apple');
  });

  it('does NOT recognize a sense addition mixed with an edit (must not hide the edit)', () => {
    expect(recognizeTreeCommit(
      [changeEntity({'$type': 'CreateSenseChange'}), changeEntity({'$type': 'jsonPatch:Entry'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e1'}],
      ['CreateSenseChange', 'jsonPatch:Entry'],
    )).toBeNull();
  });

  it('does not recognize a single change', () => {
    expect(recognizeTreeCommit([changeEntity({'$type': 'CreateEntryChange'})], [{rootEntryId: 'e1'}], ['CreateEntryChange'])).toBeNull();
  });

  it('does not recognize same-entity edits (they get listed, keeping the headword)', () => {
    expect(recognizeTreeCommit(
      [changeEntity({'$type': 'jsonPatch:Entry'}), changeEntity({'$type': 'jsonPatch:Sense'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e1'}],
      ['jsonPatch:Entry', 'jsonPatch:Sense'],
    )).toBeNull();
  });

  it('does not recognize a diverse commit', () => {
    expect(recognizeTreeCommit(
      [changeEntity({'$type': 'AddComplexFormTypeChange'}), changeEntity({'$type': 'CreateSenseChange'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e2'}],
      ['AddComplexFormTypeChange', 'CreateSenseChange'],
    )).toBeNull();
  });
});

describe('recognizeBulkCreate', () => {
  it('collapses a bulk vocab import to a count', () => {
    const result = recognizeBulkCreate(
      [changeEntity({'$type': 'CreateSemanticDomainChange'}), changeEntity({'$type': 'CreateSemanticDomainChange'}), changeEntity({'$type': 'CreateSemanticDomainChange'})],
      [{}, {}, {}],
      ['CreateSemanticDomainChange'],
    );
    expect(result?.fact).toEqual({kind: 'bulkCreate', noun: 'semanticDomains', count: 3});
  });

  it('collapses a bulk entry import (different entries) to a count', () => {
    const result = recognizeBulkCreate(
      [changeEntity({'$type': 'CreateEntryChange'}), changeEntity({'$type': 'CreateEntryChange'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e2'}],
      ['CreateEntryChange'],
    );
    expect(result?.fact).toEqual({kind: 'bulkCreate', noun: 'entries', count: 2});
  });

  it('does not collapse creates that all build one entry tree (those group under the headword instead)', () => {
    expect(recognizeBulkCreate(
      [changeEntity({'$type': 'CreateSenseChange'}), changeEntity({'$type': 'CreateSenseChange'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e1'}],
      ['CreateSenseChange'],
    )).toBeNull();
  });

  it('does not collapse a single change', () => {
    expect(recognizeBulkCreate([changeEntity({'$type': 'CreateEntryChange'})], [{rootEntryId: 'e1'}], ['CreateEntryChange'])).toBeNull();
  });

  it('has a bulk noun for every create change type, so any batch collapses', () => {
    const createTypes = knownChangeTypes.filter((type) =>
      describeChange(changeEntity({'$type': type})).some((f) => f.kind === 'create' || f.kind === 'createObject'),
    );
    const uncollapsed = createTypes.filter((type) =>
      recognizeBulkCreate([changeEntity({'$type': type}), changeEntity({'$type': type})], [{}, {}], [type])?.fact.kind !== 'bulkCreate',
    );
    expect(uncollapsed).toEqual([]);
  });
});

describe('summarizeActivity', () => {
  function patchChange() {
    return changeEntity({'$type': 'jsonPatch:Entry', PatchDocument: [{op: 'replace', path: '/LexemeForm/en', value: 'x'}]});
  }

  it('caps the listed facts and counts the left-off changes', () => {
    const overCap = ROW_FACT_CAP + 5;
    const changes = Array.from({length: overCap}, patchChange);
    const info = changes.map((_, i) => ({rootEntryId: `e${i}`})); // different roots → not collapsed
    const result = summarizeActivity(changes, info, ['jsonPatch:Entry']);
    expect(result.entries.length).toBe(ROW_FACT_CAP);
    expect(result.remaining).toBe(overCap - ROW_FACT_CAP);
  });

  it('renders the change that fills the fact budget whole, so remaining counts whole changes', () => {
    function multiFactChange() {
      return changeEntity({
        '$type': 'jsonPatch:Entry',
        PatchDocument: Array.from({length: 4}, (_, i) => ({op: 'replace', path: `/LexemeForm/ws${i}`, value: 'x'})),
      });
    }
    const result = describeActivityCapped([multiFactChange(), multiFactChange(), patchChange()], undefined, 6);
    expect(result.entries.length).toBe(8); // second change crosses the budget but is not truncated
    expect(result.remaining).toBe(1);
  });

  it('lists an entry-creation commit instead of collapsing it, so it groups like the same tree in a bigger commit', () => {
    const result = summarizeActivity(
      [changeEntity({'$type': 'CreateEntryChange'}), changeEntity({'$type': 'CreateSenseChange'})],
      [{subject: 'Apfel', rootEntryId: 'e1'}, {subject: 'Apfel › apple', rootEntryId: 'e1'}],
      ['CreateEntryChange', 'CreateSenseChange'],
    );
    expect(result.remaining).toBe(0);
    expect(result.entries.map((e) => e.fact)).toMatchObject([
      {kind: 'create', entity: 'entry'},
      {kind: 'create', entity: 'sense'},
    ]);
  });

  it('collapses a bulk create to one counted line', () => {
    const result = summarizeActivity(
      [changeEntity({'$type': 'CreateEntryChange'}), changeEntity({'$type': 'CreateEntryChange'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e2'}],
      ['CreateEntryChange'],
    );
    expect(result.entries).toHaveLength(1);
    expect(result.remaining).toBe(0);
    expect(result.entries[0].fact).toEqual({kind: 'bulkCreate', noun: 'entries', count: 2});
  });
});

describe('groupByRootEntry', () => {
  const f = (rootEntryId: string | undefined, rootEntryHeadword?: string, subject?: string): ChangeFactWithSubject =>
    ({fact: {kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', value: 'x'}, subject, rootEntryId, rootEntryHeadword});

  it('groups mixed entry/sense/example facts of one entry tree, whatever their subjects', () => {
    const groups = groupByRootEntry([
      f('e1', 'nyumba', 'nyumba'),
      f('e1', 'nyumba', 'nyumba › house'),
      f('e1', 'nyumba', 'nyumba › 2 hut'),
    ]);
    expect(groups).toHaveLength(1);
    expect(groups[0].headword).toBe('nyumba');
    expect(groups[0].facts.map((e) => e.subject)).toEqual(['nyumba', 'nyumba › house', 'nyumba › 2 hut']);
  });

  it('merges non-adjacent facts of the same root, keeping first-occurrence group order', () => {
    const groups = groupByRootEntry([f('e1', 'a'), f('e2', 'b'), f('e1', 'a')]);
    expect(groups.map((g) => g.headword)).toEqual(['a', 'b']);
    expect(groups[0].facts).toHaveLength(2);
    expect(groups[1].facts).toHaveLength(1);
  });

  it('still groups by root when the headword is missing, normalizing empty to undefined', () => {
    const groups = groupByRootEntry([f('e1', undefined), f('e1', '')]);
    expect(groups).toHaveLength(1);
    expect(groups[0].headword).toBeUndefined();
    expect(groups[0].facts).toHaveLength(2);
  });

  it('never groups facts without a root entry, even with identical subjects', () => {
    const groups = groupByRootEntry([f(undefined, undefined, 'Noun'), f(undefined, undefined, 'Noun')]);
    expect(groups).toHaveLength(2);
    expect(groups.every((g) => g.headword === undefined && g.facts.length === 1)).toBe(true);
  });

  it('promotes the root entry\'s create to the group lead, keeping the other facts listed', () => {
    const create: ChangeFactWithSubject = {fact: {kind: 'create', entity: 'entry', label: 'nyumba'}, subject: 'nyumba', rootEntryId: 'e1', rootEntryHeadword: 'nyumba'};
    const groups = groupByRootEntry([create, f('e1', 'nyumba'), f('e1', 'nyumba')]);
    expect(groups).toHaveLength(1);
    expect(groups[0].lead).toBe(create);
    expect(groups[0].facts).toHaveLength(2);
    expect(groups[0].headword).toBe('nyumba');
  });

  it('promotes a root entry delete, but never a sense create', () => {
    const del: ChangeFactWithSubject = {fact: {kind: 'delete', entity: 'entry'}, subject: 'nyumba', rootEntryId: 'e1'};
    expect(groupByRootEntry([f('e1'), del])[0].lead).toBe(del);
    const senseCreate: ChangeFactWithSubject = {fact: {kind: 'create', entity: 'sense', label: 'house'}, rootEntryId: 'e1'};
    const groups = groupByRootEntry([senseCreate, f('e1')]);
    expect(groups[0].lead).toBeUndefined();
    expect(groups[0].facts).toHaveLength(2);
  });

  it('keeps a solo create as the lead with no listed facts', () => {
    const create: ChangeFactWithSubject = {fact: {kind: 'create', entity: 'entry'}, rootEntryId: 'e1'};
    const groups = groupByRootEntry([create]);
    expect(groups[0].lead).toBe(create);
    expect(groups[0].facts).toHaveLength(0);
  });
});

describe('factCategory', () => {
  it('maps facts to add/remove/change/reorder categories', () => {
    expect(factCategory({kind: 'create', entity: 'entry'})).toBe('added');
    expect(factCategory({kind: 'addItem', entity: 'sense', fieldId: 'semanticDomains'})).toBe('added');
    expect(factCategory({kind: 'delete', entity: 'entry'})).toBe('removed');
    expect(factCategory({kind: 'clearField', entity: 'entry', fieldId: 'citationForm'})).toBe('removed');
    expect(factCategory({kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', value: 'x'})).toBe('changed');
    expect(factCategory({kind: 'reorder', collection: 'senses'})).toBe('reordered');
    expect(factCategory({kind: 'componentLink', action: 'remove'})).toBe('removed');
    expect(factCategory({kind: 'mediaResource', action: 'delete'})).toBe('removed');
    expect(factCategory({kind: 'generic', text: 'x'})).toBe('other');
  });
});

describe('commitBadge', () => {
  function summary(...facts: ChangeFact[]): {entries: ChangeFactWithSubject[]; remaining: number} {
    return {entries: facts.map((fact) => ({fact})), remaining: 0};
  }

  it('colours structural creates and deletes', () => {
    expect(commitBadge(summary({kind: 'create', entity: 'entry'}))).toEqual({category: 'added', structural: true});
    expect(commitBadge(summary({kind: 'delete', entity: 'sense'}))).toEqual({category: 'removed', structural: true});
    expect(commitBadge(summary({kind: 'bulkCreate', noun: 'entries', count: 100}))).toEqual({category: 'added', structural: true});
    expect(commitBadge(summary({kind: 'deleteObject', object: 'publication'}))).toEqual({category: 'removed', structural: true});
  });

  it('classifies content-level facts without colour', () => {
    expect(commitBadge(summary({kind: 'addItem', entity: 'sense', fieldId: 'semanticDomains'}))).toEqual({category: 'added', structural: false});
    expect(commitBadge(summary({kind: 'clearField', entity: 'entry', fieldId: 'note'}))).toEqual({category: 'removed', structural: false});
    expect(commitBadge(summary({kind: 'create', entity: 'example'}))).toEqual({category: 'added', structural: false});
    expect(commitBadge(summary({kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', value: 'x'}))).toEqual({category: 'changed', structural: false});
    expect(commitBadge(summary({kind: 'reorder', collection: 'senses'}))).toEqual({category: 'reordered', structural: false});
  });

  it('gives mixed or unrecognized commits no badge', () => {
    expect(commitBadge(summary(
      {kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', value: 'x'},
      {kind: 'delete', entity: 'sense'},
    ))).toBeUndefined();
    expect(commitBadge({entries: summary({kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', value: 'x'}).entries, remaining: 12})).toBeUndefined();
    expect(commitBadge(summary({kind: 'generic', text: 'Create remote resource'}))).toBeUndefined();
  });
});
