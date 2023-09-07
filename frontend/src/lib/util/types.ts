import type { Primitive } from 'type-fest';
import type { Redirect } from '@sveltejs/kit';

export type PrimitiveRecord = Record<string, Primitive>;
type IndexableObject<T = unknown> = unknown extends T ? PrimitiveRecord & object : T;

export function isObject(v: unknown): v is IndexableObject {
  return v !== undefined && v !== null && typeof v === 'object';
}

export function isObjectWhere<T = unknown>(value: unknown, predicate: (value: IndexableObject<T>) => boolean): boolean {
  return isObject(value) && predicate(value as IndexableObject<T>);
}

export function isRedirect(error: unknown): error is Redirect {
  return isObjectWhere<Redirect>(error, obj => obj.status !== undefined && obj.location !== undefined);
}
