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
  #entryVersions = new SvelteMap<number, number>();
  #idToIndex = new SvelteMap<string, number>();
  #pendingBatches = new SvelteMap<number, Promise<IEntry[]>>();
  #loadedBatches = new SvelteSet<number>();

  // Generation counter to invalidate in-flight async operations after reset
  #generation = 0;

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
        void this.loadInitialCount();
      }
    );
  }

  /**
   * Get an entry from cache by index (synchronous).
   * Returns undefined if not cached.
   */
  getCachedEntryByIndex(index: number): IEntry | undefined {
    return this.#entryCache.get(index);
  }

  async #countEntries(): Promise<number> {
    const api = this.#deps.miniLcmApi();
    if (!api) return 0;
    const filterOptions = this.#buildFilterOptions();
    const search = this.#deps.search();
    return api.countEntries(search || undefined, filterOptions);
  }

  /**
   * Get (from cache) or load an entry by index (asynchronous).
   * Fetches the batch containing this index if not already loaded.
   * Returns undefined if entry not found (e.g., reset occurred during load).
   */
  async getOrLoadEntryByIndex(index: number): Promise<IEntry | undefined> {
    const cached = this.#entryCache.get(index);
    if (cached) return cached;

    await this.#loadBatchForIndex(index);

    // Return from cache. If reset occurred during load, batch wasn't cached, so this returns undefined.
    return this.#entryCache.get(index);
  }

  /**
   * Load the total count of entries matching current filters.
   * This is used to scaffold this service, so it dictates the loading state.
   */
  async loadInitialCount(): Promise<void> {
    const api = this.#deps.miniLcmApi();
    if (!api) {
      this.totalCount = undefined;
      this.loading = false;
      return;
    }

    this.loading = true;
    this.error = undefined;

    const generation = this.#generation;
    try {
      const count = await this.#countEntries();
      if (this.#generation !== generation) return; // Stale result after reset
      this.totalCount = count;
    } catch (e) {
      if (this.#generation !== generation) return; // Stale result after reset
      this.error = e instanceof Error ? e : new Error(String(e));
      this.totalCount = undefined;
    } finally {
      if (this.#generation === generation) {
        this.loading = false;
      }
    }
  }

  /**
   * Get the version of an entry by index.
   * Increments whenever the entry is updated in-place.
   */
  getVersion(index: number): number {
    return this.#entryVersions.get(index) ?? 0;
  }

  /**
   * Get the index of an entry by ID.
   * Checks the local cache first, then queries the backend.
   * Returns -1 if the entry is not found.
   */
  async getOrLoadEntryIndex(id: string): Promise<number> {
    const cached = this.#idToIndex.get(id);
    if (cached !== undefined) return cached;

    const api = this.#deps.miniLcmApi();
    if (!api) return -1;

    const queryOptions = this.#buildQueryOptions(0, 0);
    const search = this.#deps.search();

    return await api.getEntryIndex(id, search || undefined, queryOptions);
  }

  /**
   * Remove an entry by ID (for delete events).
   * Refreshes the list to keep indices in sync.
   */
  removeEntryById(id: string): void {
    const index = this.#idToIndex.get(id);

    if (index === undefined) {
      // Entry not in cache — we don't know where it was, so we MUST reset
      // or we'll have an incorrect totalCount.
      this.reset();
      void this.loadInitialCount();
      return;
    }

    // Remove from maps
    this.#idToIndex.delete(id);
    this.#entryCache.delete(index);
    this.#entryVersions.delete(index);

    // Shift all subsequent entries down by 1
    this.#shiftCache(index, -1);

    // Decrement total count
    if (this.totalCount !== undefined && this.totalCount > 0) {
      this.totalCount--;
    }

    this.#recalculateLoadedBatches();
  }

  /**
   * Update an entry in cache (for update events).
   * Note: This method is called for both entry updates AND entry creates (via EntryChanged event).
   * We detect creates vs updates by comparing the new count to our cached count.
   */
  async updateEntry(entry: IEntry): Promise<void> {
    const cachedIndex = this.#idToIndex.get(entry.id);

    if (cachedIndex !== undefined) {
      // Update the cache and increment version
      this.#entryCache.set(cachedIndex, entry);
      this.#entryVersions.set(cachedIndex, (this.#entryVersions.get(cachedIndex) ?? 1) + 1);
      return;
    }

    // Entry not in cache — we don't know if it was updated or created
    // Query backend for its current index AND the new count.
    const generation = this.#generation;
    const [newIndex, newCount] = await Promise.all([
      this.getOrLoadEntryIndex(entry.id),
      this.#countEntries()
    ]);

    if (this.#generation !== generation) return; // Stale result after reset

    if (newIndex < 0) {
      // Not found (might not match current filters)
      return;
    }

    // Detect if this is a CREATE or UPDATE by comparing counts
    const isNewEntry = this.totalCount !== undefined && newCount > this.totalCount;

    const cacheIndices = [...this.#entryCache.keys()];
    const maxLoadedIndex = Math.max(...cacheIndices, -1);
    const minLoadedIndex = cacheIndices.length > 0 ? Math.min(...cacheIndices) : Infinity;

    if (newIndex > maxLoadedIndex) {
      // It's beyond our currently loaded range.
      if (isNewEntry) {
        // It's a CREATE - increment the total count so scroll height updates
        this.totalCount = newCount;
      }
      // For updates, do nothing - the entry will load naturally when scrolled to.
      return;
    }

    if (newIndex < minLoadedIndex) {
      // It's BEFORE our loaded range
      if (isNewEntry) {
        // New entry inserted before our range - shift cached entries up
        this.#shiftCache(newIndex, 1);
        this.totalCount = newCount;
        this.#recalculateLoadedBatches();
      }
      // For updates that moved before our range, do nothing special
      return;
    }

    // It's within our loaded range but not in cache (could be a hole from skipped batches).
    if (isNewEntry) {
      // New entry inserted within our range - shift and add to cache
      this.#shiftCache(newIndex, 1);
      this.#entryCache.set(newIndex, entry);
      this.#idToIndex.set(entry.id, newIndex);
      this.#entryVersions.set(newIndex, 1);
      this.totalCount = newCount;
      this.#recalculateLoadedBatches();
    }
    // For updates that moved into our range from elsewhere, we don't handle this case perfectly.
    // The entry will appear when the batch is reloaded.
  }

  /**
   * Shift indices in the cache.
   * @param fromIndex The starting index (inclusive)
   * @param delta The amount to shift (e.g. -1 for delete, 1 for insert)
   */
  #shiftCache(fromIndex: number, delta: number): void {
    const newCache = new SvelteMap<number, IEntry>();
    const newIdToIndex = new SvelteMap<string, number>();
    const newVersions = new SvelteMap<number, number>();

    // Copy unaffected entries (those before fromIndex if delta > 0, or all if delta < 0)
    for (const [idx, entry] of this.#entryCache) {
      let targetIdx = idx;
      if (idx >= fromIndex) {
        targetIdx += delta;
      }

      // Only keep in cache if the index is valid (>= 0)
      if (targetIdx >= 0) {
        newCache.set(targetIdx, entry);
        newIdToIndex.set(entry.id, targetIdx);
        newVersions.set(targetIdx, this.#entryVersions.get(idx) ?? 1);
      }
    }

    this.#entryCache = newCache;
    this.#idToIndex = newIdToIndex;
    this.#entryVersions = newVersions;
  }

  /**
   * Full reset: clear all cache and refetch count.
   */
  reset(): void {
    this.#generation++;
    this.#entryCache.clear();
    this.#entryVersions.clear();
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
    const generation = this.#generation;
    const promise = this.#fetchBatch(batchNumber);
    this.#pendingBatches.set(batchNumber, promise);

    try {
      const entries = await promise;
      if (this.#generation !== generation) return; // Stale result after reset
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
      // Only set version if not already present (so we don't reset versions on reload)
      if (!this.#entryVersions.has(index)) {
        this.#entryVersions.set(index, 1);
      }
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
    // After shifting indices, recalculate which batches are still fully loaded.
    // A batch is "loaded" only if ALL its indices are present in cache.
    this.#loadedBatches.clear();

    const maxIndex = Math.max(...this.#entryCache.keys(), -1);
    if (maxIndex < 0) return;

    const maxBatch = Math.floor(maxIndex / this.batchSize);
    for (let batch = 0; batch <= maxBatch; batch++) {
      const startIndex = batch * this.batchSize;
      const endIndex = Math.min(startIndex + this.batchSize, this.totalCount ?? this.batchSize);

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
