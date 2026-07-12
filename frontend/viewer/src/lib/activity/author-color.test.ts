import {describe, it, expect} from 'vitest';
import {AUTHOR_COLORS, assignAuthorColors, authorColorFallback} from './author-color';

describe('assignAuthorColors', () => {
  it('gives every author a distinct colour when the set fits the palette', () => {
    const names = ['Alice', 'Bob', 'Carol', 'Dave', 'Eve', 'Frank', 'Grace', 'Heidi'];
    expect(names.length).toBe(AUTHOR_COLORS.length);
    const colors = assignAuthorColors(names);
    expect(new Set(colors.values()).size).toBe(names.length);
  });

  it('is order-independent: any ordering of the same set yields the same assignment', () => {
    const names = ['Alice', 'Bob', 'Carol', 'Dave'];
    const forward = assignAuthorColors(names);
    const reversed = assignAuthorColors([...names].reverse());
    const shuffled = assignAuthorColors(['Carol', 'Alice', 'Dave', 'Bob']);
    for (const name of names) {
      expect(reversed.get(name)).toBe(forward.get(name));
      expect(shuffled.get(name)).toBe(forward.get(name));
    }
  });

  it('preserves the fallback colour for names whose hash slot is uncontested', () => {
    // Pick names that hash to distinct slots so none is bumped by a probe.
    const names = ['Alice', 'Bob', 'Carol', 'Dave'];
    const slots = names.map(n => authorColorFallback(n));
    expect(new Set(slots).size).toBe(names.length);
    const colors = assignAuthorColors(names);
    for (const name of names) {
      expect(colors.get(name)).toBe(authorColorFallback(name));
    }
  });

  it('resolves a collision deterministically by probing to a free slot', () => {
    // Search for two names that hash to the same slot to exercise probing.
    const bySlot = new Map<string, string[]>();
    for (let i = 0; i < 400; i++) {
      const name = `user${i}`;
      const slot = authorColorFallback(name);
      (bySlot.get(slot) ?? bySlot.set(slot, []).get(slot)!).push(name);
    }
    const colliding = [...bySlot.values()].find(group => group.length >= 2)!;
    expect(colliding).toBeDefined();
    const [a, b] = colliding;
    expect(authorColorFallback(a)).toBe(authorColorFallback(b));

    const colors = assignAuthorColors([a, b]);
    // Both are still assigned, and to different colours (the second probed off the shared slot).
    expect(colors.get(a)).not.toBe(colors.get(b));
    // The name sorting first keeps the shared hash slot.
    const first = [a, b].sort()[0];
    expect(colors.get(first)).toBe(authorColorFallback(first));
  });

  it('degrades gracefully beyond the palette: all names assigned, colours reused deterministically', () => {
    const names = Array.from({length: AUTHOR_COLORS.length + 5}, (_, i) => `author-${i}`);
    const colors = assignAuthorColors(names);
    expect(colors.size).toBe(names.length);
    for (const name of names) {
      expect(AUTHOR_COLORS).toContain(colors.get(name));
    }
    // A full palette means every colour is in use.
    expect(new Set(colors.values()).size).toBe(AUTHOR_COLORS.length);
    // Deterministic: re-running yields the identical map.
    const again = assignAuthorColors([...names].reverse());
    for (const name of names) expect(again.get(name)).toBe(colors.get(name));
  });

  it('deduplicates repeated names', () => {
    const colors = assignAuthorColors(['Alice', 'Alice', 'Bob']);
    expect(colors.size).toBe(2);
  });
});
