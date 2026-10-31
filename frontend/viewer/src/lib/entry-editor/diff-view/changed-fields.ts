import type {FieldId} from '$lib/views/entity-config';
import type {TypedViewField} from '$lib/views/view-data';

type CompareAs<T extends FieldId, M> = {[K in T & keyof M]?: (value: M[K]) => unknown};

/** 'changes' shows only what a change did, 'full' every field the view shows. */
export type DiffMode = 'changes' | 'full';

/**
 * The view's fields, with the hidden ones this change touched shown, or the diff would look empty.
 * `compareAs` reduces a field to what its diff renders, so e.g. a renamed but still selected item doesn't count.
 */
export function showChangedFields<T extends FieldId, M extends object>(
  fields: TypedViewField<T>[], before: M | undefined, after: M | undefined, compareAs: CompareAs<T, M> = {},
): TypedViewField<T>[] {
  const changed = changedFieldIds(fields, before, after, compareAs);
  return fields.map((field) => field.show || !changed.has(field.fieldId) ? field : {...field, show: true});
}

/** The fields whose diff would show a change, compared like {@link showChangedFields}, whether shown or not. */
export function changedFieldIds<T extends FieldId, M extends object>(
  fields: TypedViewField<T>[], before: M | undefined, after: M | undefined, compareAs: CompareAs<T, M> = {},
): Set<T> {
  const compared = comparer(compareAs);
  return new Set(fields
    .filter((field) => valueChanged(compared(before, field.fieldId), compared(after, field.fieldId)))
    .map((field) => field.fieldId));
}

/**
 * For 'changes' mode: the shown fields narrowed to the changed ones, and how many shown fields that kept a value
 * were left out. Unchanged empty fields aren't counted: revealing them shows nothing.
 */
export function changesOnly<T extends FieldId, M extends object>(
  fields: TypedViewField<T>[], changed: Set<T>, after: M | undefined, compareAs: CompareAs<T, M> = {},
): {fields: TypedViewField<T>[]; unchanged: number} {
  const compared = comparer(compareAs);
  const unchanged = fields.filter((field) => field.show && !changed.has(field.fieldId)
    && valueChanged(undefined, compared(after, field.fieldId))).length;
  return {fields: fields.map((field) => field.show && !changed.has(field.fieldId) ? {...field, show: false} : field), unchanged};
}

function comparer<T extends FieldId, M>(compareAs: CompareAs<T, M>): (model: M | undefined, fieldId: string) => unknown {
  const projections = compareAs as Record<string, ((value: unknown) => unknown) | undefined>;
  return (model, fieldId) => {
    const value = (model as Record<string, unknown> | undefined)?.[fieldId];
    const project = projections[fieldId];
    return value === undefined || value === null || !project ? value : project(value);
  };
}

export function ids(list: readonly {id: string}[]): string[] {
  return list.map((item) => item.id);
}

export function sortedIds(list: readonly {id: string}[]): string[] {
  return ids(list).sort();
}

export function valueChanged(before: unknown, after: unknown): boolean {
  return JSON.stringify(normalize(before) ?? null) !== JSON.stringify(normalize(after) ?? null);
}

// Like the diffs render them: rich text compares as plain text, and an empty value as a missing one.
function normalize(value: unknown): unknown {
  if (value === null || value === undefined || value === '') return undefined;
  if (Array.isArray(value)) return value.length ? value.map(normalize) : undefined;
  if (typeof value !== 'object') return value;
  if (isRichString(value)) return normalize(value.spans.map((span) => span.text).join(''));
  const entries = Object.entries(value)
    .map(([key, v]) => [key, normalize(v)] as const)
    .filter(([, v]) => v !== undefined)
    .sort(([x], [y]) => (x < y ? -1 : 1));
  return entries.length ? Object.fromEntries(entries) : undefined;
}

function isRichString(value: object): value is {spans: {text?: string}[]} {
  return 'spans' in value && Array.isArray(value.spans);
}
