const AVATAR_COLORS = [
  'oklch(55% 0.17 245)',
  'oklch(53% 0.15 160)',
  'oklch(50% 0.2 275)',
  'oklch(55% 0.16 30)',
  'oklch(52% 0.14 200)',
  'oklch(54% 0.15 320)',
  'oklch(51% 0.16 100)',
  'oklch(53% 0.14 350)',
] as const;

function hashString(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash * 31 + value.charCodeAt(i)) | 0;
  }
  return Math.abs(hash);
}

export function getAuthorInitials(authorName?: string): string {
  const parts = (authorName ?? '').trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase();
  return `${parts[0]![0] ?? ''}${parts[1]![0] ?? ''}`.toUpperCase();
}

export function getAuthorColor(authorId?: string, authorName?: string): string {
  const key = authorId || authorName || '?';
  return AVATAR_COLORS[hashString(key) % AVATAR_COLORS.length]!;
}
