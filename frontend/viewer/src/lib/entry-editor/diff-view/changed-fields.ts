import type {FieldId} from '$lib/views/entity-config';
import type {TypedViewField} from '$lib/views/view-data';

type CompareAs<T extends FieldId, M> = {[K in T & keyof M]?: (value: M[K]) => unknown};

/**
 * The view's fields, with the hidden ones this change touched shown, or the diff would look empty.
 * `compareAs` reduces a field to what its diff renders, so e.g. a renamed but still selected item doesn't count.
 */
export function showChangedFields<T extends FieldId, M extends object>(
  fields: TypedViewField<T>[], before: M | undefined, after: M | undefined, compareAs: CompareAs<T, M> = {},
): TypedViewField<T>[] {
  const projections = compareAs as Record<string, ((value: unknown) => unknown) | undefined>;
  function compared(model: M | undefined, fieldId: string): unknown {
    const value = (model as Record<string, unknown> | undefined)?.[fieldId];
    const project = projections[fieldId];
    return value === undefined || value === null || !project ? value : project(value);
  }
  return fields.map((field) => field.show || !valueChanged(compared(before, field.fieldId), compared(after, field.fieldId))
    ? field
    : {...field, show: true});
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
