import type {ISense} from '$lib/dotnet-types';
import type {WritingSystemService} from '$project/data';
import type {View} from '$lib/views/view-data';
import type {ReadonlyDeep} from 'type-fest';

/**
 * A sense of an entry, with its number among the entry's senses (only when it has several).
 * 'deleted' when the entry is live but no longer has the sense; undefined when there's no sense id or the
 * entry is gone (a deleted entry's snapshot has no senses).
 */
export function senseInEntry<S extends {id: string}>(
  entry: {deletedAt?: string; senses?: readonly S[]} | undefined,
  senseId: string | undefined,
): {sense: S; number?: number} | 'deleted' | undefined {
  if (!senseId || !entry || entry.deletedAt) return undefined;
  const senses = entry.senses ?? [];
  const index = senses.findIndex(s => s.id === senseId);
  if (index < 0) return 'deleted';
  return {sense: senses[index], number: senses.length > 1 ? index + 1 : undefined};
}

/** "2 house": the number, then the gloss or else the definition, preferring the view's. Undefined when there's neither. */
export function senseLabel(
  writingSystemService: WritingSystemService,
  view: View,
  {sense, number}: {sense: ReadonlyDeep<ISense>; number?: number},
): string | undefined {
  const text = writingSystemService.viewFirstGlossOrDef(sense, view);
  return [number, text].filter(Boolean).join(' ') || undefined;
}
