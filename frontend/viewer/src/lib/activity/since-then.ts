import type {IEntry, IObjectWithId} from '$lib/dotnet-types';
import {changedFieldIds, valueChanged} from '$lib/entry-editor/diff-view/changed-fields';
import {entryCompareAs, exampleCompareAs, senseCompareAs} from '$lib/entry-editor/diff-view/compare-as';
import {isObjectType, objectDiffRows} from '$lib/entry-editor/diff-view/diff-fields';
import type {View} from '$lib/views/view-data';

export type SinceThenStatus = 'unchanged' | 'changed' | 'deleted';

export type SinceThen = {
  status: SinceThenStatus;
  /** The state right after the change, undefined if it was deleted */
  before?: IObjectWithId;
  /** The state now, undefined if deleted */
  after?: IObjectWithId;
};

// A deleted entity's snapshot is its last state marked deleted: the diff shows it as gone.
function existing(snapshot: IObjectWithId | undefined): IObjectWithId | undefined {
  return snapshot && !snapshot.deletedAt ? snapshot : undefined;
}

/**
 * How the entity stands now compared to right after a change, or undefined when there is no "Later changes"
 * view: no diff for this type, or no current state. Gone both then and now counts as unchanged.
 * "Changed" is decided like the Diff*Primitive components do, so the label and the diff agree.
 */
export function sinceThen(
  entityType: string,
  snapshot: IObjectWithId | undefined,
  currentSnapshot: IObjectWithId | undefined,
  view: Pick<View, 'entryFields' | 'senseFields' | 'exampleFields'>,
): SinceThen | undefined {
  if (!currentSnapshot) return undefined;
  const hasDiff = ['Entry', 'Sense', 'ExampleSentence'].includes(entityType) || isObjectType(entityType);
  if (!hasDiff) return undefined;

  const before = existing(snapshot);
  const after = existing(currentSnapshot);
  if (!before && !after) return {status: 'unchanged', before, after};
  if (!after) return {status: 'deleted', before, after};
  if (!before) return {status: 'changed', before, after};
  return {status: hasVisibleChange(entityType, before, after, view) ? 'changed' : 'unchanged', before, after};
}

function hasVisibleChange(
  entityType: string,
  before: IObjectWithId,
  after: IObjectWithId,
  view: Pick<View, 'entryFields' | 'senseFields' | 'exampleFields'>,
): boolean {
  switch (entityType) {
    case 'Entry': {
      const b = before as IEntry, a = after as IEntry;
      return changedFieldIds(view.entryFields, b, a, entryCompareAs).size > 0
        || b.morphType !== a.morphType
        || (b.homographNumber ?? 0) !== (a.homographNumber ?? 0);
    }
    case 'Sense':
      return changedFieldIds(view.senseFields, before as never, after as never, senseCompareAs).size > 0;
    case 'ExampleSentence':
      return changedFieldIds(view.exampleFields, before as never, after as never, exampleCompareAs).size > 0;
    default: {
      if (!isObjectType(entityType)) return false;
      const b = before as unknown as Record<string, unknown>, a = after as unknown as Record<string, unknown>;
      return Object.entries(objectDiffRows[entityType]).some(([field, row]) => !!row && valueChanged(b[field], a[field]));
    }
  }
}
