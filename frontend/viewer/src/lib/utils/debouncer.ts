/**
 * Promise-aware debounce utility. Multiple callers can await the eventual execution.
 */

import debounce from 'lodash.debounce';

export interface DebouncerOptions {
  wait: number;
  leading?: boolean;
  trailing?: boolean;
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

  cancel(): void {
    this.#debounced.cancel();
    this.#resolve?.(undefined as T);
    this.#cleanup();
  }

  flush(): Promise<T> | undefined {
    if (!this.#promise) return undefined;
    this.#debounced.flush();
    return this.#promise;
  }

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
