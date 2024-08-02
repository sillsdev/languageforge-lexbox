import { writable, type Readable, derived } from 'svelte/store';

export const enum Duration {
  Persistent = 0,
  Default = 5000,
  Medium = 10000,
  Long = 15000,
}

export async function delay<T>(ms: Duration | number = Duration.Default): Promise<T> {
  return new Promise<T>(resolve => setTimeout(resolve, ms));
}

const DEFAULT_DEBOUNCE_TIME = 400;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
interface Debouncer<P extends any[]> {
  debounce: (...args: P) => void;
  debouncing: Readable<boolean>;
  clear: () => void;
}

function pickDebounceTime(debounce: number | boolean): number {
  return typeof debounce === 'number' ? debounce : DEFAULT_DEBOUNCE_TIME;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function makeDebouncer<P extends any[]>(fn: Debouncer<P>['debounce'], debounce: number | boolean = DEFAULT_DEBOUNCE_TIME): Debouncer<P> {
  const debouncing = writable(false);

  if (!debounce) {
    return { debounce: fn, debouncing, clear: () => { } };
  } else {
    const debounceTime = pickDebounceTime(debounce);
    let timeout: ReturnType<typeof setTimeout>;
    return {
      debounce: (...args: P) => {
        debouncing.set(true);
        clearTimeout(timeout);
        timeout = setTimeout(() => {
          try {
            fn(...args);
          } finally {
            debouncing.set(false);
          }
        }, debounceTime);
      },
      debouncing,
      clear: () => {
        clearTimeout(timeout);
        debouncing.set(false);
      },
    };
  }
}

/**
 * @param fn A function that maps the store value to an async result
 * @returns A store that contains the result of the async function, optionally debounced
 */
export function deriveAsync<T, D>(
  store: Readable<T>,
  fn: (value: T) => Promise<D>,
  initialValue?: D,
  debounce: number | boolean = false): Readable<D> {

  const debounceTime = pickDebounceTime(debounce);
  let timeout: ReturnType<typeof setTimeout> | undefined;

  return derived(store, (value, set) => {
    clearTimeout(timeout);
    timeout = setTimeout(() => {
      const myTimeout = timeout;
      void fn(value).then((result) => {
        if (myTimeout !== timeout) return; // discard outdated results
        set(result);
      });
    }, debounceTime);
  }, initialValue);
}
