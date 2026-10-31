import {describe, expect, it} from 'vitest';
import type {IChangeEntity} from '$lib/dotnet-types';
import {changeBadge, changeKind, isReorder, parentCrumb, stripSenseNumber} from './change-header';

function change($type?: string): IChangeEntity {
  return {index: 0, commitId: 'c', entityId: 'e', authoredModelVersion: 1, change: {entityId: 'e', ...($type ? {$type} : {})}};
}

describe('changeKind', () => {
  it('maps the snapshot types the preview knows', () => {
    expect(changeKind('Entry')).toBe('entry');
    expect(changeKind('Sense')).toBe('sense');
    expect(changeKind('ExampleSentence')).toBe('example');
    expect(changeKind('ComplexFormComponent')).toBe('complexFormComponent');
    expect(changeKind('PartOfSpeech')).toBe('object');
    expect(changeKind('WritingSystem')).toBe('object');
    expect(changeKind('Unknown')).toBe('unknown');
    expect(changeKind('RemoteResource')).toBe('unknown');
  });
});

describe('isReorder', () => {
  it('reads the serialized type discriminator', () => {
    expect(isReorder(change('SetOrderChange:Sense'))).toBe(true);
    expect(isReorder(change('SetOrderChange:ComplexFormComponent'))).toBe(true);
    expect(isReorder(change('jsonPatch:Entry'))).toBe(false);
    expect(isReorder(change())).toBe(false);
    expect(isReorder(undefined)).toBe(false);
  });
});

describe('changeBadge', () => {
  const x = {};
  it('says what the change did beyond an edit', () => {
    expect(changeBadge('entry', {after: x, reorder: false})).toBe('added');
    expect(changeBadge('entry', {before: x, reorder: false})).toBe('deleted');
    expect(changeBadge('entry', {before: x, after: x, reorder: false})).toBeUndefined();
    expect(changeBadge('object', {before: x, reorder: false})).toBe('deleted');
  });

  it('calls a deleted component link removed', () => {
    expect(changeBadge('complexFormComponent', {before: x, reorder: false})).toBe('removed');
    expect(changeBadge('complexFormComponent', {after: x, reorder: false})).toBe('added');
  });

  it('marks a reorder as moved', () => {
    expect(changeBadge('sense', {before: x, after: x, reorder: true})).toBe('moved');
  });

  it('gives an unknown change no badge', () => {
    expect(changeBadge('unknown', {after: x, reorder: false})).toBeUndefined();
  });
});

describe('stripSenseNumber', () => {
  it('drops a trailing subscript sense number', () => {
    expect(stripSenseNumber('house₂')).toBe('house');
    expect(stripSenseNumber('house₁₂')).toBe('house');
    expect(stripSenseNumber('house')).toBe('house');
  });

  it('drops the stand-in of a sense without a gloss', () => {
    expect(stripSenseNumber('◌₂')).toBe('');
  });
});

describe('parentCrumb', () => {
  it('shows only the entry for a sense, never the sense itself', () => {
    expect(parentCrumb('sense', {subject: 'nyumba₁ › house₂', owningEntryHeadword: 'nyumba₁'})).toEqual(['nyumba₁']);
    // Sense create, delete and reorder name the entry as subject
    expect(parentCrumb('sense', {subject: 'nyumba₁', owningEntryHeadword: 'nyumba₁'})).toEqual(['nyumba₁']);
  });

  it('splits an example subject into entry and sense, keeping the homograph number', () => {
    expect(parentCrumb('example', {subject: 'nyumba₁ › house₂', owningEntryHeadword: 'nyumba₁'})).toEqual(['nyumba₁', 'house']);
    expect(parentCrumb('example', {subject: 'nyumba₁', owningEntryHeadword: 'nyumba₁'})).toEqual(['nyumba₁']);
    expect(parentCrumb('example', {subject: 'nyumba › ◌₂', owningEntryHeadword: 'nyumba'})).toEqual(['nyumba']);
  });

  it('keeps the sense when the entry has no headword', () => {
    expect(parentCrumb('example', {subject: 'house₂'})).toEqual(['house']);
    expect(parentCrumb('sense', {subject: 'house₂'})).toEqual([]);
  });

  it('names the complex form of a component link', () => {
    expect(parentCrumb('complexFormComponent', {subject: 'nyumba yanga', owningEntryHeadword: 'nyumba yanga'})).toEqual(['nyumba yanga']);
  });

  it('is empty without a subject, and for entries and objects', () => {
    expect(parentCrumb('example', {owningEntryHeadword: 'nyumba'})).toEqual([]);
    expect(parentCrumb('example', undefined)).toEqual([]);
    expect(parentCrumb('entry', {subject: 'nyumba', owningEntryHeadword: 'nyumba'})).toEqual([]);
    expect(parentCrumb('object', {subject: 'Noun'})).toEqual([]);
  });
});
