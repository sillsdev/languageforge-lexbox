import {describe, expect, it} from 'vitest';
import {formatRelativeDate} from './format-relative-date-fn.svelte';

const cfg = (now: Date, smallestUnit: 'milliseconds' | 'seconds' | 'minutes' | 'hours' = 'seconds') => ({
  defaultValue: 'NEVER',
  now,
  smallestUnit,
});

describe('formatRelativeDate', () => {
  it('returns defaultValue for nullish input', () => {
    const now = new Date();
    expect(formatRelativeDate(null, undefined, cfg(now))).toBe('NEVER');
    expect(formatRelativeDate(undefined, undefined, cfg(now))).toBe('NEVER');
  });

  it('formats a past duration', () => {
    const now = new Date();
    const result = formatRelativeDate(new Date(now.getTime() - 3000), undefined, cfg(now));
    expect(result.startsWith('3 ')).toBe(true);
    expect(result.endsWith(' ago')).toBe(true);
  });

  it('formats a future duration', () => {
    const now = new Date();
    const result = formatRelativeDate(new Date(now.getTime() + 3000), undefined, cfg(now));
    expect(result.startsWith('in 3 ')).toBe(true);
  });

  it('falls back to a zero-duration string when diff is below smallestUnit', () => {
    const now = new Date();
    expect(formatRelativeDate(new Date(now.getTime() - 500), undefined, cfg(now))).toMatch(/^0 .+ ago$/);
    expect(formatRelativeDate(new Date(now.getTime() + 500), undefined, cfg(now))).toMatch(/^in 0 /);
  });

  it('produces the correct plural for zero (style=long)', () => {
    const now = new Date();
    expect(formatRelativeDate(now, {style: 'long'}, cfg(now))).toContain('0 seconds');
  });

  it('accepts ISO-string dates', () => {
    const now = new Date();
    const earlier = new Date(now.getTime() - 3000).toISOString();
    expect(formatRelativeDate(earlier, undefined, cfg(now)).startsWith('3 ')).toBe(true);
  });
});
