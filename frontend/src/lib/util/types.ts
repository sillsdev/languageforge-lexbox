type Indexable = Record<string, string | number | boolean | undefined>;
type IndexableObject<T = unknown> = unknown extends T ? Indexable & object : T;

export function isObject(v: unknown): v is IndexableObject {
  return v !== undefined && v !== null && typeof v === 'object';
}

export const isObjectWhere = <T = unknown>(value: unknown, predicate: (value: IndexableObject<T>) => boolean): boolean => {
  return isObject(value) && predicate(value as IndexableObject<T>);
}

// https://www.typescriptlang.org/docs/handbook/mixins.html
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type MixinConstructor = new (...args: any[]) => object;
