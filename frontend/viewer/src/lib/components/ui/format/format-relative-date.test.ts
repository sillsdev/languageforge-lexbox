import {describe, expect, it} from 'vitest';

import {formatRelativeDate} from './format-relative-date-fn.svelte';

function config(now: Date, smallestUnit: 'milliseconds' | 'seconds' | 'minutes' | 'hours' = 'seconds') {
  return {defaultValue: 'NEVER', now, smallestUnit};
}

describe('formatRelativeDate', () => {
  it('returns defaultValue for nullish input', () => {
    const now = new Date();
    expect(formatRelativeDate(null, undefined, config(now))).toBe('NEVER');
    expect(formatRelativeDate(undefined, undefined, config(now))).toBe('NEVER');
  });

  it('formats a past duration', () => {
    const now = new Date();
    const result = formatRelativeDate(new Date(now.getTime() - 3000), undefined, config(now));
    expect(result.startsWith('3 ')).toBe(true);
    expect(result.endsWith(' ago')).toBe(true);
  });

  it('formats a future duration', () => {
    const now = new Date();
    const result = formatRelativeDate(new Date(now.getTime() + 3000), undefined, config(now));
    expect(result.startsWith('in 3 ')).toBe(true);
  });

  it('says "just now" when the diff is below smallestUnit', () => {
    const now = new Date();
    expect(formatRelativeDate(new Date(now.getTime() - 500), undefined, config(now))).toBe('just now');
    expect(formatRelativeDate(new Date(now.getTime() - 30_000), undefined, config(now, 'minutes'))).toBe('just now');
  });

  it('says "just now" for a future diff below smallestUnit (clock skew)', () => {
    const now = new Date();
    expect(formatRelativeDate(new Date(now.getTime() + 500), undefined, config(now))).toBe('just now');
  });

  it('keeps a numeric zero for style=digital', () => {
    const now = new Date();
    const result = formatRelativeDate(new Date(now.getTime() - 500), {style: 'digital'}, config(now));
    expect(result).toMatch(/^[\d:]+ ago$/);
  });

  it('produces the correct plural for zero (style=long)', () => {
    const now = new Date();
    expect(formatRelativeDate(now, {style: 'long', secondsDisplay: 'always'}, config(now))).toContain('0 seconds');
  });

  it('accepts ISO-string dates', () => {
    const now = new Date();
    const earlier = new Date(now.getTime() - 3000).toISOString();
    expect(formatRelativeDate(earlier, undefined, config(now)).startsWith('3 ')).toBe(true);
  });
});
