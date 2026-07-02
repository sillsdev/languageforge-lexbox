/* eslint-disable @typescript-eslint/naming-convention -- change payloads below mirror the PascalCase CRDT wire format on purpose */
import {describe, it, expect} from 'vitest';
import {describeActivity, describeChange, explicitlyHandledChangeTypes, groupBySubject, isHandledChangeType, recognizeCommit, summarizeActivity} from './change-summary';
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

describe('recognizeCommit', () => {
  it('collapses an entry-creation commit (create + its tree) to "Created entry X"', () => {
    const result = recognizeCommit(
      [changeEntity({'$type': 'CreateEntryChange'}), changeEntity({'$type': 'CreateSenseChange'})],
      [{subject: 'Apfel', rootEntryId: 'e1'}, {subject: 'Apfel › apple', rootEntryId: 'e1'}],
      ['CreateEntryChange', 'CreateSenseChange'],
    );
    expect(result?.fact).toMatchObject({kind: 'create', entity: 'entry'});
    expect(result?.subject).toBe('Apfel');
  });

  it('collapses a sense-creation commit (sense + its examples) to "Added sense X"', () => {
    const result = recognizeCommit(
      [changeEntity({'$type': 'CreateSenseChange'}), changeEntity({'$type': 'CreateExampleSentenceChange'})],
      [{subject: 'Apfel › apple', rootEntryId: 'e1'}, {subject: 'Apfel › apple', rootEntryId: 'e1'}],
      ['CreateSenseChange', 'CreateExampleSentenceChange'],
    );
    expect(result?.fact).toMatchObject({kind: 'create', entity: 'sense'});
    expect(result?.subject).toBe('Apfel › apple');
  });

  it('does NOT collapse a sense addition mixed with an edit (must not hide the edit)', () => {
    expect(recognizeCommit(
      [changeEntity({'$type': 'CreateSenseChange'}), changeEntity({'$type': 'jsonPatch:Entry'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e1'}],
      ['CreateSenseChange', 'jsonPatch:Entry'],
    )).toBeNull();
  });

  it('collapses a bulk vocab import to a count', () => {
    const result = recognizeCommit(
      [changeEntity({'$type': 'CreateSemanticDomainChange'}), changeEntity({'$type': 'CreateSemanticDomainChange'}), changeEntity({'$type': 'CreateSemanticDomainChange'})],
      [{}, {}, {}],
      ['CreateSemanticDomainChange'],
    );
    expect(result?.fact).toEqual({kind: 'bulkCreate', noun: 'semanticDomains', count: 3});
  });

  it('collapses a bulk entry import (different entries) to a count', () => {
    const result = recognizeCommit(
      [changeEntity({'$type': 'CreateEntryChange'}), changeEntity({'$type': 'CreateEntryChange'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e2'}],
      ['CreateEntryChange'],
    );
    expect(result?.fact).toEqual({kind: 'bulkCreate', noun: 'entries', count: 2});
  });

  it('does not collapse a single change', () => {
    expect(recognizeCommit([changeEntity({'$type': 'CreateEntryChange'})], [{rootEntryId: 'e1'}], ['CreateEntryChange'])).toBeNull();
  });

  it('does not collapse same-entity edits (they get listed, keeping the headword)', () => {
    expect(recognizeCommit(
      [changeEntity({'$type': 'jsonPatch:Entry'}), changeEntity({'$type': 'jsonPatch:Sense'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e1'}],
      ['jsonPatch:Entry', 'jsonPatch:Sense'],
    )).toBeNull();
  });

  it('does not collapse a diverse commit', () => {
    expect(recognizeCommit(
      [changeEntity({'$type': 'AddComplexFormTypeChange'}), changeEntity({'$type': 'CreateSenseChange'})],
      [{rootEntryId: 'e1'}, {rootEntryId: 'e2'}],
      ['AddComplexFormTypeChange', 'CreateSenseChange'],
    )).toBeNull();
  });

  it('has a bulk noun for every create change type, so any batch collapses', () => {
    const createTypes = knownChangeTypes.filter((type) =>
      describeChange(changeEntity({'$type': type})).some((f) => f.kind === 'create' || f.kind === 'createObject'),
    );
    const uncollapsed = createTypes.filter((type) =>
      recognizeCommit([changeEntity({'$type': type}), changeEntity({'$type': type})], [{}, {}], [type])?.fact.kind !== 'bulkCreate',
    );
    expect(uncollapsed).toEqual([]);
  });
});

describe('summarizeActivity', () => {
  function patchChange() {
    return changeEntity({'$type': 'jsonPatch:Entry', PatchDocument: [{op: 'replace', path: '/LexemeForm/en', value: 'x'}]});
  }

  it('caps the listed changes in detailed mode and counts the rest', () => {
    const changes = Array.from({length: 25}, patchChange);
    const info = changes.map((_, i) => ({rootEntryId: `e${i}`})); // different roots → not collapsed
    const result = summarizeActivity(changes, info, ['jsonPatch:Entry'], true);
    expect(result.entries.length).toBeLessThanOrEqual(10);
    expect(result.remaining).toBe(15);
  });

  it('shows just the first change in simple mode', () => {
    const changes = [patchChange(), patchChange()];
    const result = summarizeActivity(changes, [{rootEntryId: 'e1'}, {rootEntryId: 'e2'}], ['jsonPatch:Entry'], false);
    expect(result.entries).toHaveLength(1);
    expect(result.remaining).toBe(1);
  });

  it('collapses a recognized commit to one line in both modes', () => {
    const args = [
      [changeEntity({'$type': 'CreateEntryChange'}), changeEntity({'$type': 'CreateSenseChange'})],
      [{subject: 'Apfel', rootEntryId: 'e1'}, {subject: 'Apfel › apple', rootEntryId: 'e1'}],
      ['CreateEntryChange', 'CreateSenseChange'],
    ] as const;
    for (const detailed of [true, false]) {
      const result = summarizeActivity(args[0], args[1], args[2], detailed);
      expect(result.entries).toHaveLength(1);
      expect(result.remaining).toBe(0);
      expect(result.entries[0].fact).toMatchObject({kind: 'create', entity: 'entry'});
    }
  });
});

describe('groupBySubject', () => {
  const f = (subject: string | undefined, value = 'x') =>
    ({fact: {kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', value}, subject}) as never;

  it('merges adjacent facts with the same subject', () => {
    const groups = groupBySubject([f('gwa₁'), f('gwa₁'), f('gwa₁')]);
    expect(groups).toHaveLength(1);
    expect(groups[0].subject).toBe('gwa₁');
    expect(groups[0].facts).toHaveLength(3);
  });

  it('keeps distinct subjects in separate groups', () => {
    const groups = groupBySubject([f('run › to run'), f('run › a jog')]);
    expect(groups).toHaveLength(2);
    expect(groups.map((g) => g.subject)).toEqual(['run › to run', 'run › a jog']);
  });

  it('does not merge subjectless facts', () => {
    const groups = groupBySubject([f(undefined), f(undefined)]);
    expect(groups).toHaveLength(2);
  });

  it('preserves order and only merges adjacent runs', () => {
    const groups = groupBySubject([f('a'), f('b'), f('a')]);
    expect(groups.map((g) => g.subject)).toEqual(['a', 'b', 'a']);
  });
});
