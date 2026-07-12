// Stable per-author colour for the person icon. A small curated palette (mid-tone hues that read in both
// light and dark) — not raw HSL, which drifts into muddy/low-contrast colours. Colours only the icon.
export const AUTHOR_COLORS = [
  'text-red-500', 'text-orange-500', 'text-amber-600', 'text-green-600',
  'text-teal-500', 'text-sky-500', 'text-violet-500', 'text-pink-500',
];

export const NO_AUTHOR_COLOR = 'text-muted-foreground';

function hashSlot(name: string): number {
  let hash = 0;
  for (let i = 0; i < name.length; i++) hash = (Math.imul(hash, 31) + name.charCodeAt(i)) | 0;
  return Math.abs(hash) % AUTHOR_COLORS.length;
}

// Colour for a single name with no knowledge of the wider author set: hash to a preferred slot. This is
// what a name keeps in the set-based assignment when its slot is uncontested, so it doubles as the fallback
// before the author list has loaded.
export function authorColorFallback(name: string | undefined | null): string {
  if (!name) return NO_AUTHOR_COLOR;
  return AUTHOR_COLORS[hashSlot(name)];
}

// Assign a palette colour to every name in a known author set, maximizing distinct colours. Order-independent:
// names are processed in a canonical sort, each claiming its hash slot if free and otherwise linear-probing to
// the next free slot. With ≤ AUTHOR_COLORS.length names every colour is distinct; beyond that, slots are reused
// deterministically. A name whose hash slot is uncontested keeps exactly authorColorFallback's colour.
export function assignAuthorColors(names: Iterable<string>): Map<string, string> {
  const unique = [...new Set(names)].sort();
  const result = new Map<string, string>();
  const taken = new Set<number>();
  const paletteSize = AUTHOR_COLORS.length;
  for (const name of unique) {
    let slot = hashSlot(name);
    // Once the palette is full, reuse begins; the probe still lands deterministically on the hash slot.
    for (let i = 0; i < paletteSize && taken.has(slot) && taken.size < paletteSize; i++) {
      slot = (slot + 1) % paletteSize;
    }
    taken.add(slot);
    result.set(name, AUTHOR_COLORS[slot]);
  }
  return result;
}
