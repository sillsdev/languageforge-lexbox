import { writable, type Readable } from 'svelte/store';

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

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function makeDebouncer<P extends any[]>(fn: Debouncer<P>['debounce'], debounce: number | boolean = DEFAULT_DEBOUNCE_TIME): Debouncer<P> {
  const debouncing = writable(false);

  if (!debounce) {
    return { debounce: fn, debouncing, clear: () => { } };
  } else {
    const debounceTime = typeof debounce === 'number' ? debounce : DEFAULT_DEBOUNCE_TIME;
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
 * @param promiseFn A debounced function that returns a promise
 * @param then A callback that is called after the last active promise has resolved
 * @returns
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function makeAsyncDebouncer<P extends any[], R>(
  promiseFn: (...args: P) => Promise<R>,
  then: (result: R) => void,
  debounce: number | boolean = DEFAULT_DEBOUNCE_TIME): Debouncer<P> {
  const debouncing = writable(false);

  const debounceTime = typeof debounce === 'number' ? debounce
    : debounce ? DEFAULT_DEBOUNCE_TIME : 0;

  let timeout: ReturnType<typeof setTimeout> | undefined;

  return {
    debounce: (...args: P) => {
      debouncing.set(true);
      clearTimeout(timeout);
        timeout = setTimeout(() => {
          const myTimeout = timeout;
          void promiseFn(...args).then((result) => {
            if (myTimeout !== timeout) return; // discard outdated results
            then(result);
            debouncing.set(false);
          }).catch((error) => {
            if (myTimeout === timeout) debouncing.set(false);
            throw error;
          });
        }, debounceTime);
    },
    debouncing,
    clear: () => {
      clearTimeout(timeout);
      timeout = undefined;
      debouncing.set(false);
    },
  };
}
