import type {ResourceReturn} from 'runed';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';

/**
 * Like runed's `resource`, but uses Promise + $state instead of $effect,
 * so the fetch is not tied to (and torn down with) the creating component's lifecycle.
 * Lazy: the first fetch is deferred until `.current` is read or `.refetch()` is called.
 */
export class LazyProjectResource<T> implements ResourceReturn<T, unknown, true> {
  #current: T = $state()!;
  #loading = $state(false);
  #error: Error | undefined = $state(undefined);
  #active = false;
  #fetchVersion = 0;
  #factory: (api: IMiniLcmJsInvokable) => Promise<T>;
  #getApi: () => IMiniLcmJsInvokable | undefined;

  constructor(initialValue: T, factory: (api: IMiniLcmJsInvokable) => Promise<T>, getApi: () => IMiniLcmJsInvokable | undefined) {
    this.#current = initialValue;
    this.#factory = factory;
    this.#getApi = getApi;
  }

  get current(): T {
    if (!this.#active) {
      this.#active = true;
      if (this.#getApi()) queueMicrotask(() => {
        const api = this.#getApi();
        if (api) void this.#fetch(api);
      });
    }
    return this.#current;
  }

  get loading(): boolean {
    return this.#loading;
  }

  get error(): Error | undefined {
    return this.#error;
  }

  mutate(value: T) {
    this.#current = value;
  }

  async refetch(): Promise<T | undefined> {
    this.#active = true;
    const api = this.#getApi();
    if (!api) return this.#current;
    return this.#fetch(api);
  }

  onApiChange(api: IMiniLcmJsInvokable) {
    if (this.#active) void this.#fetch(api);
  }

  async #fetch(api: IMiniLcmJsInvokable): Promise<T> {
    const version = ++this.#fetchVersion;
    this.#loading = true;
    this.#error = undefined;
    try {
      const result = await this.#factory(api);
      if (version !== this.#fetchVersion) return result;
      this.#current = result;
      return result;
    } catch (e) {
      if (version === this.#fetchVersion) {
        this.#error = e instanceof Error ? e : new Error(String(e));
      }
      return this.#current;
    } finally {
      if (version === this.#fetchVersion) {
        this.#loading = false;
      }
    }
  }
}
