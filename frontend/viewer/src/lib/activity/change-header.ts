import type {IActivityChangeInfo, IChangeEntity} from '$lib/dotnet-types';
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

/**
 * The current names of the parents a change's entity sits under, outermost first, from the activity list's change info.
 * The info's subject reads "headword › sense label" for anything under a sense, so the owning entry's headword
 * splits it. No subject means the backend couldn't resolve it: no crumb.
 */
export function parentCrumb(kind: ChangeKind, info: IActivityChangeInfo | undefined): string[] {
  const subject = info?.subject;
  if (!subject) return [];
  const headword = info.owningEntryHeadword || undefined;
  switch (kind) {
    case 'sense':
      // Only the entry: a sense edit's subject also names the sense, which the header's identity already shows
      return headword ? [headword] : [];
    case 'example':
    case 'complexFormComponent':
      return splitSubject(subject, headword);
    default:
      return [];
  }
}

function splitSubject(subject: string, headword: string | undefined): string[] {
  if (headword && subject === headword) return [headword];
  const prefix = headword && `${headword} › `;
  if (prefix && subject.startsWith(prefix)) {
    return [headword, subject.slice(prefix.length)];
  }
  // Without a headword the subject is the sense label alone
  return [subject];
}
