import { writable, type Readable, get } from 'svelte/store';

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
interface Debouncer<P extends any[], R = void> {
  debounce: (...args: P) => R;
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

// eslint-disable-next-line @typescript-eslint/no-explicit-any
interface AsyncDebouncer<P extends any[], R = void> {
  /**
   * Queues a debounce call, returning a promise that resolves when the debounced function has been called
   */
  debounce: (...args: P) => Promise<R>;
  debouncing: Readable<boolean>;
  /**
   * Flushes the debouncer, returning a promise that resolves when the debounced function has been called
   * Only uses the passed arguments to call the debounced function if the debouncer has never been run before
   */
  flush: (...args: P) => Promise<R>;
}

/**
 * @param promiseFn A debounced function that returns a promise
 * @param then A callback that is only called after all promises have resolved
 * @returns
 */
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function makeAsyncDebouncer<P extends any[], R>(
  promiseFn: Debouncer<P, Promise<R>>['debounce'],
  then: (result: R) => void,
  debounce: number | boolean = DEFAULT_DEBOUNCE_TIME): AsyncDebouncer<P, R> {
  const debouncing = writable(false);

  const debounceTime = typeof debounce === 'number' ? debounce
    : debounce ? DEFAULT_DEBOUNCE_TIME : 0;

  let timeout: ReturnType<typeof setTimeout>;
  let openResolves: ((value: R) => void)[] = [];
  let lastResult: R | undefined;
  let flushing = false;
  let latestIsInFlight = false;

  return {
    debounce: (...args: P) => {
      debouncing.set(true);
      clearTimeout(timeout);
      latestIsInFlight = false;
      return new Promise((resolve, reject) => {
        openResolves.push(resolve);
        timeout = setTimeout(() => {
          const myTimeout = timeout;
          latestIsInFlight = true;
          void promiseFn(...args).then((result) => {
            if (myTimeout !== timeout) return;
            for (const openResolve of openResolves) openResolve(result);
            openResolves = [];
            then(result);
            lastResult = result;
            debouncing.set(false);
          }).catch((error) => {
            if (myTimeout === timeout) debouncing.set(false);
            reject(error)
            throw error;
          });
        }, flushing ? 0 : debounceTime);
      });
    },
    debouncing,
    flush: async function (...args: P) {
      return new Promise<R>((resolve) => {
        if (flushing) {
          // everything's already wired up, we'll just add ourself to the subscribers
          openResolves.push(resolve);
        }

        flushing = true;

        // we already have an up-to-date result, we'll use that
        if (!get(debouncing) && lastResult) return resolve(lastResult);
        if (latestIsInFlight) { // we're already in flight, we'll just add ourself to the subscribers
          openResolves.push(resolve);
        } else { // with flushing = true we'll force an immediate execution
          void this.debounce(...args).then(resolve);
        }
      }).finally(() => (flushing = false));
    },
  };
}
