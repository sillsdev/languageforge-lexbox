type Indexable = Record<string, string | number | boolean | undefined>;

export function isObject(v: unknown): v is object & Indexable {
  return v !== undefined && v !== null && typeof v === 'object';
}
