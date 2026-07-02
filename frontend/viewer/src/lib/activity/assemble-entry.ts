import type {IChangeContext, IEntry, IExampleSentence, IPartOfSpeech, ISense} from '$lib/dotnet-types';

/**
 * Rebuild an entry as it stood at a commit, from that commit's own change snapshots. For a "create entry"
 * commit every part (the entry, its senses, their examples) is created in the commit, so each change's
 * at-commit snapshot is that part's historical state — piecing them together gives the entry as-created,
 * not its current (possibly since-edited) state.
 *
 * Notes on fidelity:
 * - Order: senses/examples keep the order their changes occur in the commit — the `.order` field is
 *   `[MiniLcmInternal]` and stripped from the wire, so it can't be read here.
 * - Part of speech: resolved from `partsOfSpeech` when a sense snapshot carries only `partOfSpeechId`.
 * - Semantic domains: already denormalized (with names) into the sense snapshot, so no resolution needed.
 */
export function assembleEntryAtCommit(
  entryId: string,
  contexts: readonly IChangeContext[],
  partsOfSpeech: readonly IPartOfSpeech[],
): IEntry | undefined {
  // Last Entry change for this id wins (its snapshot is the entry's final state within the commit).
  const entrySnap = [...contexts].reverse().find((c) => c.entityType === 'Entry' && c.snapshot?.id === entryId)?.snapshot as IEntry | undefined;
  if (!entrySnap) return undefined;

  const exampleSnaps = latestById(contexts.filter((c) => c.entityType === 'ExampleSentence' && !!c.snapshot).map((c) => c.snapshot as IExampleSentence));
  const senseSnaps = latestById(contexts.filter((c) => c.entityType === 'Sense' && !!c.snapshot).map((c) => c.snapshot as ISense)).filter((s) => s.entryId === entryId);

  const senses = senseSnaps.map((s) => ({
    ...s,
    partOfSpeech: s.partOfSpeech ?? (s.partOfSpeechId ? partsOfSpeech.find((p) => p.id === s.partOfSpeechId) : undefined),
    pictures: s.pictures ?? [],
    exampleSentences: exampleSnaps.filter((e) => e.senseId === s.id),
  }));

  return {...entrySnap, senses};
}

/**
 * Rebuild one sense (with its examples) as it stood at a commit — for a commit that created a single sense
 * and its child examples. Same at-commit-snapshot approach as {@link assembleEntryAtCommit}.
 */
export function assembleSenseAtCommit(
  senseId: string,
  contexts: readonly IChangeContext[],
  partsOfSpeech: readonly IPartOfSpeech[],
): ISense | undefined {
  const senseSnap = [...contexts].reverse().find((c) => c.entityType === 'Sense' && c.snapshot?.id === senseId)?.snapshot as ISense | undefined;
  if (!senseSnap) return undefined;

  const exampleSnaps = latestById(contexts.filter((c) => c.entityType === 'ExampleSentence' && !!c.snapshot).map((c) => c.snapshot as IExampleSentence)).filter((e) => e.senseId === senseId);

  return {
    ...senseSnap,
    partOfSpeech: senseSnap.partOfSpeech ?? (senseSnap.partOfSpeechId ? partsOfSpeech.find((p) => p.id === senseSnap.partOfSpeechId) : undefined),
    pictures: senseSnap.pictures ?? [],
    exampleSentences: exampleSnaps,
  };
}

// Keep first-seen order (creation order within the commit) while taking each entity's latest snapshot.
function latestById<T extends {id: string}>(items: readonly T[]): T[] {
  const order: string[] = [];
  const latest = new Map<string, T>();
  for (const item of items) {
    if (!latest.has(item.id)) order.push(item.id);
    latest.set(item.id, item);
  }
  return order.map((id) => latest.get(id)!);
}
