import { get, type Readable } from 'svelte/store';

export function hasValue<T>(store: Readable<T | null | undefined> | undefined): store is Readable<T> {
  const value = store && get(store);
  return value !== null && value !== undefined;
}
