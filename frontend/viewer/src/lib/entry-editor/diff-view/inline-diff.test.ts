import {describe, it, expect} from 'vitest';
import {computeDiff, hasChanges} from './inline-diff';

describe('computeDiff', () => {
  it('marks identical text as a single equal segment', () => {
    const segments = computeDiff('hello world', 'hello world');
    expect(segments).toEqual([{value: 'hello world', type: 'equal'}]);
    expect(hasChanges(segments)).toBe(false);
  });

  it('reconstructs before and after around a replaced word', () => {
    const segments = computeDiff('the cat sat', 'the dog sat');
    expect(hasChanges(segments)).toBe(true);
    expect(segments.filter((s) => s.type !== 'added').map((s) => s.value).join('')).toBe('the cat sat');
    expect(segments.filter((s) => s.type !== 'removed').map((s) => s.value).join('')).toBe('the dog sat');
  });

  it('treats text added from empty as a single added segment', () => {
    expect(computeDiff('', 'new value')).toEqual([{value: 'new value', type: 'added'}]);
  });

  it('treats text cleared to empty as a single removed segment', () => {
    expect(computeDiff('old value', '')).toEqual([{value: 'old value', type: 'removed'}]);
  });
});
