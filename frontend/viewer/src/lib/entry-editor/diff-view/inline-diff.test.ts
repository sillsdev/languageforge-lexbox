import {afterEach, describe, it, expect, vi} from 'vitest';
import {computeDiff, hasChanges, type DiffSegment} from './inline-diff';

function rebuiltBefore(segments: DiffSegment[]): string {
  return segments.filter((s) => s.type !== 'added').map((s) => s.value).join('');
}

function rebuiltAfter(segments: DiffSegment[]): string {
  return segments.filter((s) => s.type !== 'removed').map((s) => s.value).join('');
}

describe('computeDiff', () => {
  it('marks identical text as a single equal segment', () => {
    const segments = computeDiff('hello world', 'hello world');
    expect(segments).toEqual([{value: 'hello world', type: 'equal'}]);
    expect(hasChanges(segments)).toBe(false);
  });

  it('reconstructs before and after around a replaced word', () => {
    const segments = computeDiff('the cat sat', 'the dog sat');
    expect(hasChanges(segments)).toBe(true);
    expect(rebuiltBefore(segments)).toBe('the cat sat');
    expect(rebuiltAfter(segments)).toBe('the dog sat');
  });

  it('treats text added from empty as a single added segment', () => {
    expect(computeDiff('', 'new value')).toEqual([{value: 'new value', type: 'added'}]);
  });

  it('treats text cleared to empty as a single removed segment', () => {
    expect(computeDiff('old value', '')).toEqual([{value: 'old value', type: 'removed'}]);
  });

  it.each([
    ['word', 'wórd'],
    ['áb', 'àb'],
    ['ó', 'é'],
    ['\u{1F642} hi', '\u{1F643} hi'],
    [' plain', ' pláin'],
  ])('never splits a grapheme across segments: %j → %j', (before, after) => {
    const segments = computeDiff(before, after);
    for (const {value} of segments) {
      expect(value).not.toMatch(/^\p{M}/u);
      expect(value).not.toMatch(/^[\uDC00-\uDFFF]|[\uD800-\uDBFF]$/);
    }
    expect(rebuiltBefore(segments)).toBe(before);
    expect(rebuiltAfter(segments)).toBe(after);
  });
});

describe('computeDiff without Intl.Segmenter', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.resetModules();
  });

  async function importWithoutSegmenter() {
    // Intl's members are non-enumerable, so spreading it would drop them all; inherit them instead.
    // eslint-disable-next-line @typescript-eslint/naming-convention -- the browser API's name
    vi.stubGlobal('Intl', Object.create(Intl, {Segmenter: {value: undefined}}));
    vi.resetModules();
    return (await import('./inline-diff')).computeDiff;
  }

  it('loads and diffs by code point, keeping surrogate pairs whole', async () => {
    const diffWithoutSegmenter = await importWithoutSegmenter();

    const segments = diffWithoutSegmenter('\u{1F642} hi', '\u{1F643} hi');
    for (const {value} of segments) {
      expect(value).not.toMatch(/^[\uDC00-\uDFFF]|[\uD800-\uDBFF]$/);
    }
    expect(rebuiltBefore(segments)).toBe('\u{1F642} hi');
    expect(rebuiltAfter(segments)).toBe('\u{1F643} hi');
  });

  it('splits a combining mark from its base letter', async () => {
    const diffWithoutSegmenter = await importWithoutSegmenter();

    const segments = diffWithoutSegmenter('cafe\u0301 noir', 'cafe\u0300 noir');
    // with Intl.Segmenter the whole é/è grapheme would differ, leaving only "caf" equal
    expect(segments[0]).toEqual({value: 'cafe', type: 'equal'});
    expect(rebuiltBefore(segments)).toBe('cafe\u0301 noir');
    expect(rebuiltAfter(segments)).toBe('cafe\u0300 noir');
  });
});
