import {describe, expect, it} from 'vitest';
import type {IChangeEntity} from '$lib/dotnet-types';
import {changeBadge, changeKind, componentSense, isReorder, parentCrumb} from './change-header';

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

describe('parentCrumb', () => {
  const nyumba = {id: 'e1', headword: 'nyumba', homographNumber: 1};
  const nyumbaCrumb = {text: 'nyumba', homographNumber: 1};

  it('shows only the entry for a sense, never the sense itself', () => {
    expect(parentCrumb('sense', {entry: nyumba, sense: {label: 'house', number: 2}})).toEqual([nyumbaCrumb]);
    // Sense create, delete and reorder name the entry as subject
    expect(parentCrumb('sense', {entry: nyumba})).toEqual([nyumbaCrumb]);
  });

  it('names an example\'s entry and numbered sense', () => {
    expect(parentCrumb('example', {entry: nyumba, sense: {label: 'house'}})).toEqual([nyumbaCrumb, {text: 'house'}]);
    expect(parentCrumb('example', {entry: nyumba, sense: {label: 'house', number: 2}})).toEqual([nyumbaCrumb, {text: '2 house'}]);
    expect(parentCrumb('example', {entry: nyumba, sense: {number: 2}})).toEqual([nyumbaCrumb, {text: '2'}]);
    expect(parentCrumb('example', {entry: nyumba, sense: {}})).toEqual([nyumbaCrumb]);
  });

  it('keeps the sense when the entry has no headword', () => {
    expect(parentCrumb('example', {entry: {id: 'e1', homographNumber: 0}, sense: {label: 'house'}})).toEqual([{text: 'house'}]);
    expect(parentCrumb('sense', {entry: {id: 'e1', homographNumber: 0}, sense: {label: 'house'}})).toEqual([]);
  });

  it('names the complex form of a component link', () => {
    expect(parentCrumb('complexFormComponent', {entry: nyumba})).toEqual([nyumbaCrumb]);
  });

  it('is empty when unresolved, and for entries and objects', () => {
    expect(parentCrumb('example', {})).toEqual([]);
    expect(parentCrumb('example', undefined)).toEqual([]);
    expect(parentCrumb('entry', {entry: nyumba})).toEqual([]);
    expect(parentCrumb('object', {objectName: 'Noun'})).toEqual([]);
  });
});

describe('componentSense', () => {
  const a = {id: 'a'};
  const b = {id: 'b'};

  it('numbers the sense when the entry has several', () => {
    expect(componentSense({senses: [a, b]}, 'b')).toEqual({sense: b, number: 2});
  });

  it('leaves out the number for a lone sense', () => {
    expect(componentSense({senses: [a]}, 'a')).toEqual({sense: a, number: undefined});
  });

  it('flags a sense the live entry no longer has', () => {
    expect(componentSense({senses: [a, b]}, 'gone')).toBe('deleted');
    expect(componentSense({}, 'gone')).toBe('deleted');
  });

  it('has nothing to say without a sense id, or for a missing or deleted entry', () => {
    expect(componentSense({senses: [a]}, undefined)).toBeUndefined();
    expect(componentSense(undefined, 'a')).toBeUndefined();
    expect(componentSense({deletedAt: '2026-10-02T10:00:00Z', senses: []}, 'a')).toBeUndefined();
  });
});
