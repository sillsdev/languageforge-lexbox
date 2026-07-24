import type {IPreferencesService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';

export type StoragePropOptions = {
  /** When false, skip the initial GET — useful for write-only prefs (e.g. AppLastUrl). Default true. */
  load?: boolean;
};

export class StorageProp {
  #key: string;
  #backend: IPreferencesService;
  #value = $state<string>('');
  #hasBeenSet = $state(false);

  constructor(key: string, backend: IPreferencesService, options?: StoragePropOptions) {
    this.#key = key;
    this.#backend = backend;
    if (options?.load === false) {
      this.#hasBeenSet = true;
    } else {
      void this.load();
    }
  }

  get current(): string {
    return this.#value;
  }

  get loading(): boolean {
    return !this.#hasBeenSet;
  }

  async set(value: string): Promise<void> {
    this.#hasBeenSet = true;
    this.#value = value;
    if (value) {
      await this.#backend.set(this.#key, value);
    } else {
      await this.#backend.remove(this.#key);
    }
  }

  private async load(): Promise<void> {
    try {
      const value = await this.#backend.get(this.#key);
      if (!this.#hasBeenSet) {
        this.#value = value ?? '';
      }
    } finally {
      if (!this.#hasBeenSet) {
        this.#hasBeenSet = true;
      }
    }
  }
}
