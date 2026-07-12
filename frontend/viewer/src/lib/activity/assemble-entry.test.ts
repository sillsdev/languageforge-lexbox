import {describe, it, expect} from 'vitest';
import {assembleEntryAtCommit, assembleSenseAtCommit} from './assemble-entry';
import type {IChangeContext, IEntry, IExampleSentence, IPartOfSpeech, ISense} from '$lib/dotnet-types';

function ctx(entityType: string, snapshot: object): IChangeContext {
  return {commitId: 'c', changeIndex: 0, changeName: '', entityType, snapshot, affectedEntries: []} as unknown as IChangeContext;
}

const entryId = 'entry-1';
const entrySnap = {id: entryId, lexemeForm: {en: 'run'}, senses: []} as unknown as IEntry;

describe('assembleEntryAtCommit', () => {
  it('uses the latest snapshot when an entity appears multiple times in the commit', () => {
    const contexts = [
      ctx('Entry', entrySnap),
      ctx('Sense', {id: 's1', entryId, gloss: {en: 'old gloss'}}),
      ctx('Sense', {id: 's1', entryId, gloss: {en: 'new gloss'}}),
    ];
    const entry = assembleEntryAtCommit(entryId, contexts, []);
    expect(entry?.senses).toHaveLength(1);
    expect(entry?.senses[0].gloss).toEqual({en: 'new gloss'});
  });


  it('returns undefined when the commit has no entry snapshot', () => {
    expect(assembleEntryAtCommit(entryId, [ctx('Sense', {id: 's1', entryId})], [])).toBeUndefined();
  });

  it('attaches senses (in commit order) and their examples to the entry', () => {
    const contexts = [
      ctx('Entry', entrySnap),
      ctx('Sense', {id: 's1', entryId, gloss: {en: 'to run'}}),
      ctx('ExampleSentence', {id: 'e1', senseId: 's1', sentence: {en: 'I run'}}),
      ctx('Sense', {id: 's2', entryId, gloss: {en: 'a jog'}}),
    ];
    const entry = assembleEntryAtCommit(entryId, contexts, []);
    expect(entry?.senses.map((s) => s.id)).toEqual(['s1', 's2']);
    expect(entry?.senses[0].exampleSentences.map((e: IExampleSentence) => e.id)).toEqual(['e1']);
    expect(entry?.senses[1].exampleSentences).toEqual([]);
  });

  it('resolves part of speech from the id when the snapshot only carries partOfSpeechId', () => {
    const pos: IPartOfSpeech = {id: 'pos-1', name: {en: 'Verb'}, predefined: true};
    const contexts = [ctx('Entry', entrySnap), ctx('Sense', {id: 's1', entryId, partOfSpeechId: 'pos-1'})];
    const entry = assembleEntryAtCommit(entryId, contexts, [pos]);
    expect((entry?.senses[0] as ISense).partOfSpeech).toEqual(pos);
  });

  it('ignores senses that belong to a different entry', () => {
    const contexts = [ctx('Entry', entrySnap), ctx('Sense', {id: 's1', entryId: 'other'})];
    expect(assembleEntryAtCommit(entryId, contexts, [])?.senses).toEqual([]);
  });
});

describe('assembleSenseAtCommit', () => {
  it('assembles a sense with its examples (in commit order) and resolves POS', () => {
    const pos: IPartOfSpeech = {id: 'pos-1', name: {en: 'Verb'}, predefined: true};
    const contexts = [
      ctx('Sense', {id: 's1', entryId, gloss: {en: 'to run'}, partOfSpeechId: 'pos-1'}),
      ctx('ExampleSentence', {id: 'e1', senseId: 's1'}),
      ctx('ExampleSentence', {id: 'e2', senseId: 's1'}),
      ctx('ExampleSentence', {id: 'eX', senseId: 'other'}),
    ];
    const sense = assembleSenseAtCommit('s1', contexts, [pos]);
    expect(sense?.gloss).toEqual({en: 'to run'});
    expect((sense as ISense).partOfSpeech).toEqual(pos);
    expect(sense?.exampleSentences.map((e: IExampleSentence) => e.id)).toEqual(['e1', 'e2']);
  });

  it('returns undefined when the sense snapshot is missing', () => {
    expect(assembleSenseAtCommit('s1', [ctx('ExampleSentence', {id: 'e1', senseId: 's1'})], [])).toBeUndefined();
  });
});
