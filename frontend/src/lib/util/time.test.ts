import { delay, deriveAsync } from './time';
import { describe, expect, it } from 'vitest';

import { writable } from 'svelte/store';

const debounceTime = 100;
const promiseTime = 50;

describe('deriveAsync', () => {
  it('handles standard synchronous debouncing', async () => {
    let reachedPromise = false;
    let done = false;
    let valueReceived: number | null = null;
    const store = writable<number>();
    const debouncedStore = deriveAsync(
      store,
      // the promise resolves immediately, so we're only testing the debounce that happens before awaiting the promise
      (value: number) => {
        reachedPromise = true;
        return new Promise<number>(resolve => resolve(value));
      },
      undefined,
      debounceTime);
    debouncedStore.subscribe((value) => {
      if (value !== undefined) {
        done = true;
        valueReceived = value;
      }
    });

    store.set(1);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    store.set(2); // restart the debounce
    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    store.set(3); // restart the debounce
    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    await delay(debounceTime * 0.25);
    expect(reachedPromise).toBe(true);
    expect(done).toBe(true);
    expect(valueReceived).toBe(3);
  });

  it('handles asynchronous debouncing', async () => {
    let reachedPromise = false;
    let done = false;
    let valueReceived: number | null = null;
    const store = writable<number>();
    const debouncedStore = deriveAsync(
      store,
      // the promise resolves after a delay, so it can get debounced before it hits the promise or before the promise has been resolved
      (value: number) => {
        reachedPromise = true;
        return new Promise<number>(resolve => {
          setTimeout(() => resolve(value), promiseTime);
        });
      },
      undefined,
      debounceTime);
    debouncedStore.subscribe((value) => {
      if (value !== undefined) {
        done = true;
        valueReceived = value;
      }
    });

    store.set(1);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    store.set(2); // restart the debounce
    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    store.set(3); // restart the debounce
    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    await delay(debounceTime * 0.25);
    expect(reachedPromise).toBe(true); // we hit the promise
    expect(done).toBe(false); // but it will only complete if it doesn't get debounced before the promise resolves

    await delay(promiseTime * 0.5);
    expect(done).toBe(false);

    store.set(4); // restart the debounce
    await delay(promiseTime * 0.5);
    expect(done).toBe(false);

    await delay(debounceTime + promiseTime);
    expect(done).toBe(true);
    expect(valueReceived).toBe(4);
  });
});
