import {describe, expect, it} from 'vitest';
import type {IChangeEntity, IEntry} from '$lib/dotnet-types';
import {changeBadge, componentTargets, changeKind, isReorder, parentCrumb} from './change-header';

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

describe('componentTargets', () => {
  const live = {id: 'live'} as IEntry;
  const other = {id: 'other'} as IEntry;
  const dead = {id: 'dead', deletedAt: '2026-10-02T10:00:00Z'} as IEntry;

  it('targets the complex form and offers the component', () => {
    expect(componentTargets(live, other)).toEqual({target: live, related: other});
  });

  it('targets the component when the complex form is deleted or missing', () => {
    expect(componentTargets(dead, other)).toEqual({target: other, related: undefined});
    expect(componentTargets(undefined, other)).toEqual({target: other, related: undefined});
  });

  it('offers nothing extra when the component is deleted', () => {
    expect(componentTargets(live, dead)).toEqual({target: live, related: undefined});
  });

  it('has no target when both are deleted', () => {
    expect(componentTargets(dead, dead)).toEqual({target: undefined, related: undefined});
  });
});
