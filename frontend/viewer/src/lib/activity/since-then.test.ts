import {describe, expect, it} from 'vitest';
import type {IObjectWithId} from '$lib/dotnet-types';
import {FW_LITE_VIEW} from '$lib/views/view-data';
import {sinceThen} from './since-then';

const view = FW_LITE_VIEW;
function entry(lexemeForm: Record<string, string>, extra: object = {}): IObjectWithId {
  return {id: 'e', lexemeForm, ...extra} as unknown as IObjectWithId;
}
function deleted(snapshot: IObjectWithId): IObjectWithId {
  return {...snapshot, deletedAt: '2026-01-01T00:00:00Z'};
}

describe('sinceThen', () => {
  it('is unchanged when the current value matches', () => {
    expect(sinceThen('Entry', entry({en: 'run'}), entry({en: 'run'}), view)?.status).toBe('unchanged');
  });

  it('is unchanged when only formatting differs', () => {
    function rich(text: string, bold?: string) {
      return {en: {spans: [{text, ws: 'en', ...(bold ? {bold} : {})}]}};
    }
    const a = {id: 'e', definition: rich('x')} as unknown as IObjectWithId;
    const b = {id: 'e', definition: rich('x', 'On')} as unknown as IObjectWithId;
    expect(sinceThen('Sense', a, b, view)?.status).toBe('unchanged');
  });

  it('is changed when a field differs', () => {
    expect(sinceThen('Entry', entry({en: 'run'}), entry({en: 'jog'}), view)?.status).toBe('changed');
  });

  it('is changed when only the morph type or homograph number differs', () => {
    expect(sinceThen('Entry', entry({en: 'a'}, {morphType: 'Stem'}), entry({en: 'a'}, {morphType: 'Root'}), view)?.status).toBe('changed');
    expect(sinceThen('Entry', entry({en: 'a'}, {homographNumber: 0}), entry({en: 'a'}, {homographNumber: 2}), view)?.status).toBe('changed');
  });

  it('is deleted when it was deleted later', () => {
    const snapshot = entry({en: 'run'});
    const result = sinceThen('Entry', snapshot, deleted(entry({en: 'run'})), view);
    expect(result).toEqual({status: 'deleted', before: snapshot, after: undefined});
  });

  it('is changed, from nothing, when revived', () => {
    const current = entry({en: 'run'});
    const result = sinceThen('Entry', deleted(entry({en: 'run'})), current, view);
    expect(result).toEqual({status: 'changed', before: undefined, after: current});
    expect(sinceThen('Entry', undefined, current, view)?.status).toBe('changed');
  });

  it('is unchanged when still deleted', () => {
    expect(sinceThen('Entry', deleted(entry({})), deleted(entry({})), view)?.status).toBe('unchanged');
    expect(sinceThen('Entry', undefined, deleted(entry({})), view)?.status).toBe('unchanged');
  });

  it('has nothing to show without a current snapshot', () => {
    expect(sinceThen('Entry', entry({en: 'run'}), undefined, view)).toBeUndefined();
  });

  it('has nothing to show for types without a diff', () => {
    expect(sinceThen('ComplexFormComponent', entry({}), entry({}), view)).toBeUndefined();
    expect(sinceThen('RemoteResource', entry({}), entry({}), view)).toBeUndefined();
  });

  it('compares object types by their diff rows', () => {
    function pos(name: string): IObjectWithId {
      return {id: 'p', name: {en: name}, predefined: false} as unknown as IObjectWithId;
    }
    expect(sinceThen('PartOfSpeech', pos('Noun'), pos('Noun'), view)?.status).toBe('unchanged');
    expect(sinceThen('PartOfSpeech', pos('Noun'), pos('Verb'), view)?.status).toBe('changed');
  });
});
