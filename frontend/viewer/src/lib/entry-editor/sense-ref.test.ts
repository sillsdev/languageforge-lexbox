import type {ISense} from '$lib/dotnet-types';
import type {View} from '$lib/views/view-data';
import type {WritingSystemService} from '$project/data';
import {describe, expect, it} from 'vitest';
import {senseInEntry, senseLabel} from './sense-ref';

describe('senseInEntry', () => {
  const a = {id: 'a'};
  const b = {id: 'b'};

  it('numbers the sense when the entry has several', () => {
    expect(senseInEntry({senses: [a, b]}, 'b')).toEqual({sense: b, number: 2});
  });

  it('leaves out the number for a lone sense', () => {
    expect(senseInEntry({senses: [a]}, 'a')).toEqual({sense: a, number: undefined});
  });

  it('flags a sense the live entry no longer has', () => {
    expect(senseInEntry({senses: [a, b]}, 'gone')).toBe('deleted');
    expect(senseInEntry({}, 'gone')).toBe('deleted');
  });

  it('has nothing to say without a sense id, or for a missing or deleted entry', () => {
    expect(senseInEntry({senses: [a]}, undefined)).toBeUndefined();
    expect(senseInEntry(undefined, 'a')).toBeUndefined();
    expect(senseInEntry({deletedAt: '2026-10-02T10:00:00Z', senses: []}, 'a')).toBeUndefined();
  });
});

describe('senseLabel', () => {
  const view = {id: 'lite'} as unknown as View;
  const sense = {gloss: {}} as unknown as ISense;
  function serviceShowing(text: string): WritingSystemService {
    return {viewFirstGlossOrDef: (_: ISense, v: View) => (v === view ? text : 'wrong view')} as unknown as WritingSystemService;
  }

  it("leads with the number, then the view's gloss or definition", () => {
    expect(senseLabel(serviceShowing('house'), view, {sense, number: 2})).toBe('2 house');
    expect(senseLabel(serviceShowing('house'), view, {sense})).toBe('house');
  });

  it('is undefined with neither a number nor text', () => {
    expect(senseLabel(serviceShowing(''), view, {sense})).toBeUndefined();
  });
});
