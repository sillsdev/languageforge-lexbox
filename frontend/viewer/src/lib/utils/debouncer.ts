/**
 * Debouncer - A promise-aware debounce utility built on lodash.debounce.
 *
 * Unlike raw lodash.debounce, this tracks promise completion so multiple
 * callers can await the eventual execution.
 *
 * Usage:
 *   const debouncer = new Debouncer(() => fetchData(), { wait: 300 });
 *   const promise1 = debouncer.call(); // Schedules execution
 *   const promise2 = debouncer.call(); // Resets timer, returns same promise
 *   await promise1; // Both resolve when fetchData() completes
 */

import debounce from 'lodash.debounce';

export interface DebouncerOptions {
  /** Milliseconds to wait before executing */
  wait: number;
  /** Execute on the leading edge (immediately on first call) */
  leading?: boolean;
  /** Execute on the trailing edge (after wait period). Default: true */
  trailing?: boolean;
  /** Maximum time to wait before forced execution */
  maxWait?: number;
}

export class Debouncer<T = void> {
  readonly #fn: () => T | Promise<T>;
  readonly #debounced: ReturnType<typeof debounce<() => void>>;

  #promise?: Promise<T>;
  #resolve?: (value: T) => void;
  #reject?: (error: unknown) => void;

  constructor(fn: () => T | Promise<T>, options: DebouncerOptions) {
    this.#fn = fn;

    this.#debounced = debounce(
      () => void this.#execute(),
      options.wait,
      {
        leading: options.leading ?? false,
        trailing: options.trailing ?? true,
        maxWait: options.maxWait,
      }
    );
  }

  /**
   * Schedule the debounced function. Returns a promise that resolves
   * when execution completes. Multiple calls return the same promise.
   */
  call(): Promise<T> {
    if (!this.#promise) {
      this.#promise = new Promise((resolve, reject) => {
        this.#resolve = resolve;
        this.#reject = reject;
      });
    }
    this.#debounced();
    return this.#promise;
  }

  /**
   * Cancel any pending execution. Resolves the current promise with undefined.
   */
  cancel(): void {
    this.#debounced.cancel();
    // Resolve with undefined rather than rejecting to avoid unhandled rejections
    this.#resolve?.(undefined as T);
    this.#cleanup();
  }

  /**
   * Immediately execute if there's a pending call, bypassing the timer.
   */
  flush(): Promise<T> | undefined {
    if (!this.#promise) return undefined;
    this.#debounced.flush();
    return this.#promise;
  }

  /**
   * Whether there's a pending debounced call.
   */
  get pending(): boolean {
    return this.#promise !== undefined;
  }

  async #execute(): Promise<void> {
    const resolve = this.#resolve;
    const reject = this.#reject;
    this.#cleanup();

    try {
      const result = await this.#fn();
      resolve?.(result);
    } catch (e) {
      reject?.(e);
    }
  }

  #cleanup(): void {
    this.#promise = undefined;
    this.#resolve = undefined;
    this.#reject = undefined;
  }
}
