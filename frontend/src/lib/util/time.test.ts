import { delay, makeAsyncDebouncer } from './time';
import { describe, expect, it } from 'vitest';

const debounceTime = 100;
const promiseTime = 50;

describe('AsyncDebouncer', () => {
  it('handles standard synchronous debouncing', async () => {
    let reachedPromise = false;
    let done = false;
    let valueReceived: number | null = null;
    const debouncer = makeAsyncDebouncer(
      // the promise resolves immediately, so we're only testing the debounce that happens before awaiting the promise
      (value: number) => {
        reachedPromise = true;
        return new Promise<number>(resolve => resolve(value));
      },
      (value) => {
        done = true;
        valueReceived = value;
      },
      debounceTime);

    void debouncer.debounce(1);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    void debouncer.debounce(2); // restart the debounce
    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    void debouncer.debounce(3); // restart the debounce
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
    const debouncer = makeAsyncDebouncer(
      // the promise resolves after a delay, so it can get debounced before it hits the promise or before the promise has been resolved
      (value: number) => {
        reachedPromise = true;
        return new Promise<number>(resolve => {
          setTimeout(() => resolve(value), promiseTime);
        });
      },
      (value) => {
        done = true;
        valueReceived = value;
      },
      debounceTime);

    void debouncer.debounce(1);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    void debouncer.debounce(2); // restart the debounce
    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    void debouncer.debounce(3); // restart the debounce
    await delay(debounceTime * 0.75);
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    await delay(debounceTime * 0.25);
    expect(reachedPromise).toBe(true); // we hit the promise
    expect(done).toBe(false); // but it will only complete if it doesn't get debounced before the promise resolves

    await delay(promiseTime * 0.5);
    expect(done).toBe(false);

    void debouncer.debounce(4); // restart the debounce
    await delay(promiseTime * 0.5);
    expect(done).toBe(false);

    await delay(debounceTime + promiseTime);
    expect(done).toBe(true);
    expect(valueReceived).toBe(4);
  });
});
