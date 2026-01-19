import {SvelteMap, SvelteSet} from 'svelte/reactivity';

import type {IEntry} from '$lib/dotnet-types';
import type {IFilterQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IFilterQueryOptions';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import type {IQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
import type {SortConfig} from '$project/browse/sort/options';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
import {watch} from 'runed';

export interface EntryLoaderDeps {
  miniLcmApi: () => IMiniLcmJsInvokable | undefined;
  search: () => string;
  sort: () => SortConfig | undefined;
  gridifyFilter: () => string | undefined;
}

/**
 * Service for loading entries on-demand with batch caching.
 * See ENTRY_LOADER_PLAN.md for architecture details.
 */
export class EntryLoaderService {
  // Reactive state
  totalCount = $state<number | undefined>();
  loading = $state(true);
  error = $state<Error | undefined>();

  // Cache (private)
  #entryCache = new SvelteMap<number, IEntry>();
  #idToIndex = new SvelteMap<string, number>();
  #pendingBatches = new SvelteMap<number, Promise<IEntry[]>>();
  #loadedBatches = new SvelteSet<number>();

  // Config
  readonly batchSize = 50;

  // Dependencies
  readonly #deps: EntryLoaderDeps;

  constructor(deps: EntryLoaderDeps) {
    this.#deps = deps;

    // Watch for dependency changes and reset
    watch(
      () => [deps.miniLcmApi(), deps.search(), deps.sort(), deps.gridifyFilter()],
      () => {
        this.reset();
        void this.loadCount();
      }
    );
  }

  /**
   * Get an entry from cache by index (synchronous).
   * Returns undefined if not cached.
   */
  getEntryByIndex(index: number): IEntry | undefined {
    return this.#entryCache.get(index);
  }

  /**
   * Load an entry by index (asynchronous).
   * Fetches the batch containing this index if not already loaded.
   */
  async loadEntryByIndex(index: number): Promise<IEntry> {
    // Check cache first
    const cached = this.#entryCache.get(index);
    if (cached) return cached;

    // Load the batch
    await this.#loadBatchForIndex(index);

    // Return from cache (should now be populated)
    const entry = this.#entryCache.get(index);
    if (!entry) {
      throw new Error(`Entry at index ${index} not found after loading batch`);
    }
    return entry;
  }

  /**
   * Load the total count of entries matching current filters.
   */
  async loadCount(): Promise<void> {
    const api = this.#deps.miniLcmApi();
    if (!api) {
      this.totalCount = undefined;
      this.loading = false;
      return;
    }

    this.loading = true;
    this.error = undefined;

    try {
      const filterOptions = this.#buildFilterOptions();
      const search = this.#deps.search();
      this.totalCount = await api.countEntries(search || undefined, filterOptions);
    } catch (e) {
      this.error = e instanceof Error ? e : new Error(String(e));
      this.totalCount = undefined;
    } finally {
      this.loading = false;
    }
  }

  /**
   * Get the index of an entry by ID (from our incremental map).
   * Returns undefined if the entry hasn't been loaded yet.
   */
  getIndexById(id: string): number | undefined {
    return this.#idToIndex.get(id);
  }

  /**
   * Check if an entry ID is known (loaded into cache).
   */
  hasEntry(id: string): boolean {
    return this.#idToIndex.has(id);
  }

  /**
   * Remove an entry by ID (for delete events).
   * If not in cache, triggers a full reset.
   */
  removeEntryById(id: string): void {
    const index = this.#idToIndex.get(id);

    if (index === undefined) {
      // Entry not in cache — we don't know where it was, so reset
      this.reset();
      void this.loadCount();
      return;
    }

    // Remove from maps
    this.#idToIndex.delete(id);
    this.#entryCache.delete(index);

    // Shift all subsequent entries down by 1
    const newCache = new SvelteMap<number, IEntry>();
    const newIdToIndex = new SvelteMap<string, number>();

    for (const [idx, entry] of this.#entryCache) {
      if (idx < index) {
        newCache.set(idx, entry);
        newIdToIndex.set(entry.id, idx);
      } else {
        // Shift down
        newCache.set(idx - 1, entry);
        newIdToIndex.set(entry.id, idx - 1);
      }
    }

    this.#entryCache = newCache;
    this.#idToIndex = newIdToIndex;

    // Update loaded batches (they may have shifted)
    this.#recalculateLoadedBatches();

    // Decrement total count
    if (this.totalCount !== undefined && this.totalCount > 0) {
      this.totalCount--;
    }
  }

  /**
   * Update an entry in cache (for update events).
   * If not in cache, triggers a full reset.
   */
  updateEntry(entry: IEntry): void {
    const index = this.#idToIndex.get(entry.id);

    if (index === undefined) {
      // Entry not in cache — could be new or just not loaded
      // For V1, we reset to be safe
      this.reset();
      void this.loadCount();
      return;
    }

    // Update the cache
    this.#entryCache.set(index, entry);
  }

  /**
   * Full reset: clear all cache and refetch count.
   */
  reset(): void {
    this.#entryCache.clear();
    this.#idToIndex.clear();
    this.#pendingBatches.clear();
    this.#loadedBatches.clear();
    this.totalCount = undefined;
    this.error = undefined;
    this.loading = true;
  }

  // Private methods

  async #loadBatchForIndex(index: number): Promise<void> {
    const batchNumber = Math.floor(index / this.batchSize);

    // Already loaded?
    if (this.#loadedBatches.has(batchNumber)) return;

    // Already pending?
    const pending = this.#pendingBatches.get(batchNumber);
    if (pending) {
      await pending;
      return;
    }

    // Start the fetch
    const promise = this.#fetchBatch(batchNumber);
    this.#pendingBatches.set(batchNumber, promise);

    try {
      const entries = await promise;
      this.#cacheBatch(batchNumber, entries);
      this.#loadedBatches.add(batchNumber);
    } finally {
      this.#pendingBatches.delete(batchNumber);
    }
  }

  async #fetchBatch(batchNumber: number): Promise<IEntry[]> {
    const api = this.#deps.miniLcmApi();
    if (!api) return [];

    const offset = batchNumber * this.batchSize;
    const queryOptions = this.#buildQueryOptions(offset, this.batchSize);
    const search = this.#deps.search();

    if (search) {
      return api.searchEntries(search, queryOptions);
    } else {
      return api.getEntries(queryOptions);
    }
  }

  #cacheBatch(batchNumber: number, entries: IEntry[]): void {
    const startIndex = batchNumber * this.batchSize;

    for (let i = 0; i < entries.length; i++) {
      const index = startIndex + i;
      const entry = entries[i];
      this.#entryCache.set(index, entry);
      this.#idToIndex.set(entry.id, index);
    }
  }

  #buildFilterOptions(): IFilterQueryOptions {
    const gridifyFilter = this.#deps.gridifyFilter();
    return {
      filter: gridifyFilter ? { gridifyFilter } : undefined,
    };
  }

  #buildQueryOptions(offset: number, count: number): IQueryOptions {
    const sort = this.#deps.sort();
    return {
      count,
      offset,
      filter: this.#buildFilterOptions().filter,
      order: {
        field: sort?.field ?? SortField.SearchRelevance,
        writingSystem: 'default',
        ascending: sort?.dir !== 'desc',
      },
    };
  }

  #recalculateLoadedBatches(): void {
    // After shifting indices, recalculate which batches are considered "loaded"
    // A batch is loaded if all its indices are in the cache
    this.#loadedBatches.clear();

    const maxIndex = Math.max(...this.#entryCache.keys(), -1);
    if (maxIndex < 0) return;

    const maxBatch = Math.floor(maxIndex / this.batchSize);
    for (let batch = 0; batch <= maxBatch; batch++) {
      const startIndex = batch * this.batchSize;
      const endIndex = Math.min(startIndex + this.batchSize, (this.totalCount ?? startIndex + this.batchSize));
      let allPresent = true;

      for (let i = startIndex; i < endIndex; i++) {
        if (!this.#entryCache.has(i)) {
          allPresent = false;
          break;
        }
      }

      if (allPresent) {
        this.#loadedBatches.add(batch);
      }
    }
  }
}
