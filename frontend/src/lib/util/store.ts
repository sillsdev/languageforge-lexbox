import { derived, get, type Readable } from 'svelte/store';
import type {Pausable} from '@urql/svelte';

/**
 * Tries to make a store non-nullable, which requires the store to currently have a non-null value.
 * @param store a store that may emit null values.
 * @returns if the store has a non-null value, a store that emits that value and any future non-null value.
 * Otherwise, undefined, because the store can't be made non-nullable.
 */
export function tryMakeNonNullable<T>(store: (Readable<T | null | undefined> & Pausable) | undefined): (Readable<NonNullable<T>> & Pausable) | undefined
export function tryMakeNonNullable<T>(store: (Readable<T | null | undefined>) | undefined): (Readable<NonNullable<T>>) | undefined {
  if (!store) return undefined;
  const value = get(store);
  if (value === null || value === undefined) return undefined;
  return {
    //this is to ensure that the store is pausable
    ...store,
    ...derived(store, (value, set) => {
    if (value !== undefined && value !== null) set(value);
  })};
}
