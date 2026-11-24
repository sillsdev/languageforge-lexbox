export type AuthorColor = {icon: string; avatar: string};

// One hue per slot, used for both the activity person icon and the comment avatar. Icons are mid-tones that
// read on light and dark; avatars are a shade darker so white initials stay legible. Eight is about as many
// hues as stay distinguishable at icon size, so beyond eight authors we share slots evenly instead.
export const AUTHOR_COLORS: readonly AuthorColor[] = [
  {icon: 'text-red-500', avatar: 'bg-red-600'},
  {icon: 'text-orange-500', avatar: 'bg-orange-600'},
  {icon: 'text-amber-600', avatar: 'bg-amber-700'},
  {icon: 'text-green-600', avatar: 'bg-green-700'},
  {icon: 'text-teal-500', avatar: 'bg-teal-700'},
  {icon: 'text-sky-500', avatar: 'bg-sky-700'},
  {icon: 'text-violet-500', avatar: 'bg-violet-600'},
  {icon: 'text-pink-500', avatar: 'bg-pink-600'},
];

export const NO_AUTHOR_COLOR: AuthorColor = {icon: 'text-muted-foreground', avatar: 'bg-muted-foreground'};

function hashSlot(key: string): number {
  let hash = 0;
  for (let i = 0; i < key.length; i++) hash = (Math.imul(hash, 31) + key.charCodeAt(i)) | 0;
  return Math.abs(hash) % AUTHOR_COLORS.length;
}

// Colour for an author key with no knowledge of the wider author set. It's what assignAuthorColors gives a key
// whose hash slot is uncontested, so it doubles as the colour shown before the author list loads.
export function authorColorFallback(key: string | undefined | null): AuthorColor {
  if (!key) return NO_AUTHOR_COLOR;
  return AUTHOR_COLORS[hashSlot(key)];
}

// Keys must arrive oldest author first: each takes the least-used slot, starting from its hash slot. Later
// authors can only take what's left, so adding an author never changes an earlier author's colour.
export function assignAuthorColors(keysOldestFirst: Iterable<string>): Map<string, AuthorColor> {
  const result = new Map<string, AuthorColor>();
  const usage = AUTHOR_COLORS.map(() => 0);
  for (const key of keysOldestFirst) {
    if (result.has(key)) continue;
    const leastUsed = Math.min(...usage);
    let slot = hashSlot(key);
    while (usage[slot] !== leastUsed) slot = (slot + 1) % usage.length;
    usage[slot]++;
    result.set(key, AUTHOR_COLORS[slot]);
  }
  return result;
}
