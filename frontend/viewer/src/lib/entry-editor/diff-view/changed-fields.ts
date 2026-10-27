import type {FieldId} from '$lib/views/entity-config';
import type {TypedViewField} from '$lib/views/view-data';

/** The view's fields, with the hidden ones this change touched shown, or the diff would look empty. */
export function showChangedFields<T extends FieldId>(fields: TypedViewField<T>[], before?: object, after?: object): TypedViewField<T>[] {
  const b = before as Record<string, unknown> | undefined;
  const a = after as Record<string, unknown> | undefined;
  return fields.map((field) => field.show || !valueChanged(b?.[field.fieldId], a?.[field.fieldId]) ? field : {...field, show: true});
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
