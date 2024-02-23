import { delay, makeAsyncDebouncer } from './time';
import { describe, expect, it } from 'vitest';

const debounceTime = 100;
const promiseTime = 50;

describe('AsyncDebouncer', () => {
  it('handles standard sync debouncing', async () => {
    let reachedPromise = false;
    let done = false;
    const debouncer = makeAsyncDebouncer(
      // the promise resolves immediately, so we're only testing the debounce that happens before awaiting the promise
      (value: number) => {
        reachedPromise = true;
        return new Promise(resolve => resolve(value));
      },
      () => (done = true),
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
    const result = await debouncer.flush(3);
    expect(result).toBe(3);
  });

  it('handles async debouncing', async () => {
    let reachedPromise = false;
    let done = false;
    const debouncer = makeAsyncDebouncer(
      // the promise resolves after a delay, so it can get debounced before it hits the promise or before the promise has been resolved
      (value: number) => {
        reachedPromise = true;
        return new Promise(resolve => {
          setTimeout(() => resolve(value), promiseTime);
        });
      },
      () => (done = true),
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

    void debouncer.debounce(3); // restart the debounce
    await delay(promiseTime * 0.5);
    expect(done).toBe(false);

    await delay(debounceTime + promiseTime);
    expect(done).toBe(true);

    const result = await debouncer.flush(3);
    expect(result).toBe(3);
  });

  it('flushes any active queued debounces', async () => {
    let reachedPromise = false;
    let done = false;
    const debouncer = makeAsyncDebouncer(
      (value: number) => {
        reachedPromise = true;
        return new Promise(resolve => {
          setTimeout(() => resolve(value), promiseTime);
        });
      },
      () => (done = true),
      debounceTime);

    let awaiterComplete = false;
    void debouncer.debounce(1).then(() => (awaiterComplete = true));
    expect(reachedPromise).toBe(false);
    expect(done).toBe(false);

    // we only just started waiting, but we can flush, skipping the debounce and resolve all awaiters
    const result = await Promise.any([
      debouncer.flush(2),
      delay(promiseTime * 1.5), // simply demonstrates that this completes at least as fast as promiseTime * 1.5, which is less than debounceTime
    ]);
    expect(result).toBe(2);
    expect(promiseTime * 1.5).toBeLessThan(debounceTime);
    expect(awaiterComplete).toBe(true);
    expect(reachedPromise).toBe(true);
    expect(done).toBe(true);
  });
});
