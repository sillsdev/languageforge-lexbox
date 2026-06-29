import {describe, it, expect} from 'vitest';
import {describeActivity, describeChange, isHandledChangeType, pickHeadline, type ChangeFact, type ChangeFactWithSubject} from './change-summary';
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

  it('describes entry creation with the headword', () => {
    const facts = describeChange(changeEntity({'$type': 'CreateEntryChange', LexemeForm: {en: 'apple'}, EntityId: 'e'}));
    expect(facts).toEqual([{kind: 'create', entity: 'entry', label: 'apple'}]);
  });

  it('describes sense creation with the gloss', () => {
    const facts = describeChange(changeEntity({'$type': 'CreateSenseChange', Gloss: {en: 'fruit'}, EntityId: 'e'}));
    expect(facts).toEqual([{kind: 'create', entity: 'sense', label: 'fruit'}]);
  });

  it('describes deletion', () => {
    expect(describeChange(changeEntity({'$type': 'delete:Entry', EntityId: 'e'})))
      .toEqual([{kind: 'delete', entity: 'entry'}]);
  });

  it('falls back to a humanized type for unrecognized changes', () => {
    expect(describeChange(changeEntity({'$type': 'SomeFutureChange', EntityId: 'e'})))
      .toEqual([{kind: 'generic', text: 'Some future'}]);
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

  it('describes moving a sense', () => {
    expect(describeChange(changeEntity({'$type': 'MoveSenseToEntryChange', EntryId: 'x', EntityId: 'e'})))
      .toEqual([{kind: 'moveSense'}]);
  });

  it('describes creating a vocabulary object with its name', () => {
    expect(describeChange(changeEntity({'$type': 'CreatePartOfSpeechChange', Name: {en: 'Noun'}, EntityId: 'e'})))
      .toEqual([{kind: 'createObject', object: 'partOfSpeech', label: 'Noun'}]);
  });

  // Resource/media sync changes that intentionally use the generic humanized fallback (not user-facing edits).
  const INTENTIONALLY_GENERIC = new Set<string>([
    'create:pendingUpload',
    'create:remote-resource',
    'uploaded:RemoteResource',
    'delete:RemoteResource',
  ]);

  it('handles every backend change type (generated list) or allows it as intentionally generic', () => {
    const unhandled = knownChangeTypes.filter((type) => !isHandledChangeType(type) && !INTENTIONALLY_GENERIC.has(type));
    expect(unhandled).toEqual([]);
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
});

describe('pickHeadline', () => {
  function entry(fact: ChangeFact, subject?: string, rootEntryId?: string): ChangeFactWithSubject {
    return {fact, subject, rootEntryId};
  }

  it('collapses a create-entry tree to the entry creation', () => {
    const headline = pickHeadline([
      entry({kind: 'create', entity: 'entry', label: 'Apfel'}, 'Apfel', 'e1'),
      entry({kind: 'create', entity: 'sense', label: 'apple'}, 'Apfel › apple', 'e1'),
    ]);
    expect(headline.entry.fact).toMatchObject({kind: 'create', entity: 'entry'});
    expect(headline.remaining).toBe(0);
  });

  it('leads with the first change and counts the rest when entities differ', () => {
    const headline = pickHeadline([
      entry({kind: 'setField', entity: 'entry', fieldId: 'lexemeForm', value: 'a'}, 'Apfel', 'e1'),
      entry({kind: 'setField', entity: 'sense', fieldId: 'gloss', value: 'b'}, 'Pear › p', 'e2'),
    ]);
    expect(headline.remaining).toBe(1);
  });
});
