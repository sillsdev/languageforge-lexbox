import { writable, type Readable, type Unsubscriber } from 'svelte/store';

/**
 * Unwraps the provided promise to a store
 * @param promise the promise to unwrap
 * @param storeMapper a function for mapping the awaited value to a store
 * @returns a store that will emit the values emitted from the store provided by the `storeMapper`
 */
export function unwrapToStore<T, C>(promise: Promise<T>, storeMapper: (value: T | null) => Readable<C> | undefined): Readable<C> {
  const store = writable<T | null>();
  void promise.then((value) => store.set(value));
  return {
    subscribe(run) {
      let unsub: Unsubscriber;
      return store.subscribe((value) => {
        unsub?.();
        const innerStore = storeMapper(value);
        return innerStore ? unsub = innerStore.subscribe(run) : () => undefined;
      });
    },
  };
}

/**
 * Creates a new store that filters out null and undefined
 * @param store a store to derive from
 * @returns a new store that derives from `store`, but filters out/never explicitly emits null and undefined
 */
export function filterDefined<V>(store: Readable<V>): Readable<NonNullable<V>> {
  return {
    ...store,
    subscribe(run) {
      return store.subscribe(value => {
        if (value !== undefined && value !== null) run(value);
      })
    },
  }
}
