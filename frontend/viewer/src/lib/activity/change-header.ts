import type {IActivityChangeInfo, IChangeEntity} from '$lib/dotnet-types';
import type {IActivitySenseName} from '$lib/dotnet-types/generated-types/LcmCrdt/IActivitySenseName';
import {isObjectType} from '$lib/entry-editor/diff-view/diff-fields';

/** How the activity preview names a change: each kind gets its own header. */
export type ChangeKind = 'entry' | 'sense' | 'example' | 'complexFormComponent' | 'object' | 'unknown';

export function changeKind(entityType: string): ChangeKind {
  switch (entityType) {
    case 'Entry': return 'entry';
    case 'Sense': return 'sense';
    case 'ExampleSentence': return 'example';
    case 'ComplexFormComponent': return 'complexFormComponent';
    default: return isObjectType(entityType) ? 'object' : 'unknown';
  }
}

/** The change's serialized type discriminator, e.g. "SetOrderChange:Sense", which the generated IChange type leaves out. */
function changeTypeKey(change: IChangeEntity | undefined): string | undefined {
  const type = (change?.change as {$type?: unknown} | undefined)?.$type;
  return typeof type === 'string' ? type : undefined;
}

/** A reorder only changes the entity's place in its parent's list, which none of its fields show. */
export function isReorder(change: IChangeEntity | undefined): boolean {
  return changeTypeKey(change)?.startsWith('SetOrderChange:') ?? false;
}

export type ChangeBadge = 'added' | 'deleted' | 'removed' | 'moved';

/**
 * What a change did to its entity, when that's more than an edit. No before means the change created the entity,
 * no after means it deleted it. A complex form component is a link, so deleting one removes the link.
 */
export function changeBadge(kind: ChangeKind, {before, after, reorder}: {before?: object; after?: object; reorder: boolean}): ChangeBadge | undefined {
  if (kind === 'unknown') return undefined;
  if (reorder) return 'moved';
  if (!before && after) return 'added';
  if (before && !after) return kind === 'complexFormComponent' ? 'removed' : 'deleted';
  return undefined;
}

/** A parent's current name, as text plus the homograph number of an entry. */
export type CrumbText = {text: string; homographNumber?: number};

/** "2 house": the sense's number leads, like the dictionary preview. Undefined when there's neither. */
export function senseText(sense: IActivitySenseName | undefined): string | undefined {
  if (!sense) return undefined;
  return [sense.number, sense.label].filter(Boolean).join(' ') || undefined;
}

/**
 * The parents a change's header names before its identity. A sense's entry, an example's entry and sense, a
 * component link's complex form. Empty when the backend couldn't resolve them.
 */
export function parentCrumb(kind: ChangeKind, info: IActivityChangeInfo | undefined): CrumbText[] {
  const entry = info?.entry?.headword ? {text: info.entry.headword, homographNumber: info.entry.homographNumber} : undefined;
  switch (kind) {
    case 'sense':
    case 'complexFormComponent':
      // A sense's own name is the header's identity
      return entry ? [entry] : [];
    case 'example': {
      const sense = senseText(info?.sense);
      return [...(entry ? [entry] : []), ...(sense ? [{text: sense}] : [])];
    }
    default:
      return [];
  }
}

/**
 * The sense a component link points at, with its number among the entry's senses (only when it has several).
 * 'deleted' when the entry is live but no longer has the sense; undefined when the link isn't sense-specific or
 * the entry is gone (a deleted entry's snapshot has no senses).
 */
export function componentSense<S extends {id: string}>(
  entry: {deletedAt?: string; senses?: readonly S[]} | undefined,
  senseId: string | undefined,
): {sense: S; number?: number} | 'deleted' | undefined {
  if (!senseId || !entry || entry.deletedAt) return undefined;
  const senses = entry.senses ?? [];
  const index = senses.findIndex(s => s.id === senseId);
  if (index < 0) return 'deleted';
  return {sense: senses[index], number: senses.length > 1 ? index + 1 : undefined};
}
