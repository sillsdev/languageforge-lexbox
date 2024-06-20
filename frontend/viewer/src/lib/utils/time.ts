import { writable, type Readable, derived } from 'svelte/store';

export const enum Duration {
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
  debounce: number | boolean = false): { value: Readable<D>, loading: Readable<boolean>, flush: () => void } {

  const loading = writable(false);
  const debounceTime = pickDebounceTime(debounce);
  let timeout: ReturnType<typeof setTimeout> | undefined;
  let timeoutFunction: undefined | (() => void);

  return {
    value: derived(store, (value, set) => {
      loading.set(true);
      clearTimeout(timeout);
      const myTimeoutFunction = timeoutFunction = () => {
        const myTimeout = timeout;
        if (myTimeoutFunction === timeoutFunction) timeoutFunction = undefined; // prevent it from getting triggered again
        void fn(value).then((result) => {
          if (myTimeout !== timeout) return; // discard outdated results
          loading.set(false);
          set(result);
        });
      };
      timeout = setTimeout(timeoutFunction, debounceTime);
    }, initialValue),
    loading,
    flush: () => {
      clearTimeout(timeout);
      timeoutFunction?.();
    },
  };
}
