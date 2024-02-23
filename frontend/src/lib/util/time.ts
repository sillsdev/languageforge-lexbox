import { writable, type Readable } from 'svelte/store';

export const enum Duration {
  Default = 5000,
  Medium = 10000,
  Long = 15000,
}

export async function delay<T>(ms = Duration.Default): Promise<T> {
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
export function debounce<P extends any[]>(fn: (...args: P) => void, debounce: number | boolean = DEFAULT_DEBOUNCE_TIME): Debouncer<P> {
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
