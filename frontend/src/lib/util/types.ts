import type { Redirect } from '@sveltejs/kit';

type Indexable = Record<string, string | number | boolean | undefined>;
type IndexableObject<T = unknown> = unknown extends T ? Indexable & object : T;

export function isObject(v: unknown): v is IndexableObject {
  return v !== undefined && v !== null && typeof v === 'object';
}

export const isObjectWhere = <T = unknown>(value: unknown, predicate: (value: IndexableObject<T>) => boolean): boolean => {
  return isObject(value) && predicate(value as IndexableObject<T>);
}

export const isRedirect = (error: unknown): error is Redirect => {
  return isObjectWhere<Redirect>(error, obj => obj.status !== undefined && obj.location !== undefined);
};
