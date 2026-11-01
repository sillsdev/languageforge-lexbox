import {describe, it, expect} from 'vitest';
import {AUTHOR_COLORS, assignAuthorColors, authorColorFallback, type AuthorColor} from './author-color';

function keysSharingASlot(count: number): string[] {
  const bySlot = new Map<AuthorColor, string[]>();
  for (let i = 0; ; i++) {
    const key = `user${i}`;
    const group = bySlot.get(authorColorFallback(key)) ?? [];
    group.push(key);
    bySlot.set(authorColorFallback(key), group);
    if (group.length === count) return group;
  }
}

function usageCounts(colors: Map<string, AuthorColor>): number[] {
  return AUTHOR_COLORS.map(c => [...colors.values()].filter(v => v === c).length);
}

describe('assignAuthorColors', () => {
  it('gives every author a distinct colour when the set fits the palette', () => {
    const keys = Array.from({length: AUTHOR_COLORS.length}, (_, i) => `author-${i}`);
    expect(new Set(assignAuthorColors(keys).values()).size).toBe(keys.length);
  });

  it('keeps the fallback colour for a key whose hash slot is uncontested', () => {
    const [key] = keysSharingASlot(1);
    expect(assignAuthorColors([key]).get(key)).toBe(authorColorFallback(key));
  });

  it('gives the older author the shared hash slot and moves the newer one', () => {
    const [older, newer] = keysSharingASlot(2);
    const colors = assignAuthorColors([older, newer]);
    expect(colors.get(older)).toBe(authorColorFallback(older));
    expect(colors.get(newer)).not.toBe(colors.get(older));
  });

  it('never changes existing colours when newer authors are added', () => {
    const existing = Array.from({length: 6}, (_, i) => `author-${i}`);
    const before = assignAuthorColors(existing);
    const after = assignAuthorColors([...existing, ...keysSharingASlot(3), 'late-1', 'late-2']);
    for (const key of existing) expect(after.get(key)).toBe(before.get(key));
  });

  it('spreads authors beyond the palette evenly across colours', () => {
    const keys = keysSharingASlot(AUTHOR_COLORS.length * 2 + 3);
    const counts = usageCounts(assignAuthorColors(keys));
    expect(Math.max(...counts) - Math.min(...counts)).toBeLessThanOrEqual(1);
  });

  it('ignores repeated keys after the first', () => {
    const [a, b] = keysSharingASlot(2);
    const colors = assignAuthorColors([a, b, a]);
    expect(colors.size).toBe(2);
    expect(colors.get(a)).toBe(authorColorFallback(a));
  });
});
