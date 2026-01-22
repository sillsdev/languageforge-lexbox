import {DEFAULT_DEBOUNCE_TIME, delay} from '$lib/utils/time';
import {SvelteMap, SvelteSet} from 'svelte/reactivity';

import type {IEntry} from '$lib/dotnet-types';
import type {IFilterQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IFilterQueryOptions';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import type {IQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
import type {SortConfig} from '$project/browse/sort/options';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
import {watch} from 'runed';

export interface EntryLoaderDeps {
  search: () => string;
  sort: () => SortConfig | undefined;
  gridifyFilter: () => string | undefined;
}

/**
 * Service for loading entries on-demand with batch caching.
 * See ENTRY_LOADER_PLAN.md for architecture details.
 */
export class EntryLoaderService {

  static readonly DEFAULT_GENERATION: number = 0;

  // Reactive state
  totalCount = $state<number | undefined>();
  loading = $state(true);
  error = $state<Error | undefined>();

  // Cache (private)
  #entryCache = new SvelteMap<number, IEntry>();
  #idToIndex = new SvelteMap<string, number>();
  #pendingBatches = new SvelteMap<number, Promise<IEntry[]>>();
  #loadedBatches = new SvelteSet<number>();

  // Track which batches the UI has most recently interacted with.
  // At most 2 are needed, and only if they are adjacent.
  #recentBatchNumbers: { curr?: number, prev?: number } = {};

  // Coalesce multiple quiet resets (e.g., rapid event bursts)
  #quietResetRequest = { requested: false, generation: -1 };
  #quietResetInFlight: Promise<void> | undefined;

  // Generation counter to invalidate in-flight async operations after reset
  // Must be reactive ($state) so UI can react to generation changes
  #generation = $state(-1);

  readonly #api: IMiniLcmJsInvokable;
  readonly #deps: EntryLoaderDeps;

  // Debounce timer for coalescing rapid filter changes
  #debounceTimer: ReturnType<typeof setTimeout> | undefined;

  constructor(api: IMiniLcmJsInvokable, deps: EntryLoaderDeps, readonly batchSize = 50) {
    this.#api = api;
    this.#deps = deps;

    // Initial load (no debounce on first load)
    void this.reset();

    watch(
      () => [deps.search(), deps.sort(), deps.gridifyFilter()],
      (_, oldValues) => {
        // don't reset
        if (!oldValues) return;

        // Debounce the actual fetch to coalesce rapid changes
        clearTimeout(this.#debounceTimer);
        this.#debounceTimer = setTimeout(() => {
          void this.reset();
        }, DEFAULT_DEBOUNCE_TIME);
      }
    );
  }

  /**
   * Get an entry from cache by index (synchronous).
   * Returns undefined if not cached.
   */
  getCachedEntryByIndex(index: number): IEntry | undefined {
    this.#markBatchRequested(this.#batchNumberForIndex(index));
    return this.#entryCache.get(index);
  }

  async #countEntries(): Promise<number> {
    const filterOptions = this.#buildFilterOptions();
    const search = this.#deps.search();
    return this.#api.countEntries(search || undefined, filterOptions);
  }

  /**
   * Get (from cache) or load an entry by index (asynchronous).
   * Fetches the batch containing this index if not already loaded.
   * Returns undefined if entry not found (e.g., reset occurred during load).
   */
  async getOrLoadEntryByIndex(index: number): Promise<IEntry | undefined> {
    this.#markBatchRequested(this.#batchNumberForIndex(index));
    const cached = this.#entryCache.get(index);
    if (cached) return cached;

    const generation = this.#generation;
    await this.#loadBatchForIndex(index);

    // Discard stale batches
    if (this.#generation !== generation) return undefined;

    return this.#entryCache.get(index);
  }

  /**
   * Load the total count of entries matching current filters.
   * This is used to scaffold this service, so it dictates the loading state.
   */
  async loadInitialCount(): Promise<void> {
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
   * Get the current generation of the loader.
   * Increments whenever the loader is reset (search/sort/filter changes).
   */
  get generation(): number {
    return this.#generation;
  }

  /**
   * Get the index of an entry by ID.
   * Checks the local cache first, then queries the backend.
   * Returns -1 if the entry is not found.
   */
  async getOrLoadEntryIndex(id: string): Promise<number> {
    const cached = this.#idToIndex.get(id);
    if (cached !== undefined) return cached;

    const queryOptions = this.#buildQueryOptions(0, 0);
    const search = this.#deps.search();

    const index = await this.#api.getEntryIndex(id, search || undefined, queryOptions);
    this.#idToIndex.set(id, index);
    return index;
  }

  /**
   * Remove an entry by ID (for delete events).
   * Uses a quiet reset to refresh count + the currently relevant batch(es).
   */
  removeEntryById(id: string): void {
    // We intentionally do not try to do any local shifting / heuristics.
    // The goal is to keep event handling easy to reason about.
    void this.quietReset();
  }

  /**
   * Update an entry in cache (for update events).
   * Note: This method is called for both entry updates AND entry creates (via EntryChanged event).
   * We handle both the same way: quiet reset.
   */
  async updateEntry(entry: IEntry): Promise<void> {
    const generation = this.#generation;
    if (await this.tryOptimizeUpdateEntryEvent(entry)) return;
    if (generation !== this.#generation) return; // outdated event => abort
    await this.quietReset();
  }
  
  /**
   * The more trivial and performant checks we can do to verify if the event is relevant 
   * to our current state.
   */
  private async tryOptimizeUpdateEntryEvent(entry: IEntry): Promise<boolean> {
    const cachedIndex = this.#idToIndex.get(entry.id);
    if (cachedIndex !== undefined) {
      // we've seen it locally, so it wasn't an add event
      if (cachedIndex < 0) {
        // not relevant for our current filter => ignore
        return true;
      }
      const batchIndex = Math.floor(cachedIndex / this.batchSize);
      if (!this.#loadedBatches.has(batchIndex)) {
        // it's not new and we haven't loaded it yet => ignore
        return true;
      }
    } else {
      const entryIndex = await this.getOrLoadEntryIndex(entry.id);
      if (entryIndex < 0) {
        // not relevant for our current filter => ignore
        return true;
      }
      const batchIndex = Math.floor(entryIndex / this.batchSize);
      const maxLoadedBatch = Math.max(...this.#loadedBatches);
      if (batchIndex > maxLoadedBatch) {
        // it's beyond what we've loaded, so even if it's an add event it doesn't shift anything.
        // However, the count might need to be incremented and checking that is more effort than it's worth here
        // return true;
      }
      // It's non-trivial determining if this is an add event or not.
      // We could e.g. query the current entry count and compare, but that's more effort than it's worth.
    }

    // Note: we could also:
    // - swap individual entries in place
    // - increment the count
    // - shift batches
    // but we're trying not to overcomplicate this.
    return false;
  }

  /**
   * Full reset: clear all cache and refetch count.
   */
  async reset(): Promise<void> {
    await this.loadInitialCount();
    this.#entryCache.clear();
    this.#idToIndex.clear();
    this.#pendingBatches.clear();
    this.#loadedBatches.clear();
    this.#generation++;
  }

  /**
   * Quiet reset: refresh count + reload the currently relevant batch(es), then atomically swap caches.
   * This avoids skeleton flashes for background events (add/update/delete) that affect list ordering.
   *
   * A full reset() trumps any pending quiet reset — if the generation changes during the debounce
   * window, the quiet reset is aborted.
   */
  quietReset(): Promise<void> {
    this.#quietResetRequest = { requested: true, generation: this.#generation };
    if (this.#quietResetInFlight) return this.#quietResetInFlight;

    const run = async () => {
      while (this.#quietResetRequest.requested) {
        this.#quietResetRequest.requested = false;

        // Debounce: wait for it to settle
        await delay(DEFAULT_DEBOUNCE_TIME);

        // If we've moved to a new generation: abort
        if (this.#generation !== this.#quietResetRequest.generation) {
          this.#quietResetInFlight = undefined;
          return;
        }

        if (this.#quietResetRequest.requested) continue;

        await this.#runQuietResetOnce();
      }
    };

    this.#quietResetInFlight = run().finally(() => {
      this.#quietResetInFlight = undefined;
    });

    return this.#quietResetInFlight;
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

    await this.#loadBatch(batchNumber);
  }

  async #loadBatch(batchNumber: number): Promise<void> {
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
      // Only clear if this batch is still pending with THIS promise.
      // This prevents stale loads from deleting a newer pending promise after a reset.
      if (this.#pendingBatches.get(batchNumber) === promise) {
        this.#pendingBatches.delete(batchNumber);
      }
    }
  }

  async #fetchBatch(batchNumber: number): Promise<IEntry[]> {
    const offset = batchNumber * this.batchSize;
    return this.#fetchRange(offset, this.batchSize);
  }

  async #fetchRange(offset: number, count: number): Promise<IEntry[]> {
    const queryOptions = this.#buildQueryOptions(offset, count);
    const search = this.#deps.search();

    if (search) {
      return this.#api.searchEntries(search, queryOptions);
    }
    return this.#api.getEntries(queryOptions);
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
      filter: gridifyFilter ? {gridifyFilter} : undefined,
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

  #batchNumberForIndex(index: number): number {
    return Math.floor(index / this.batchSize);
  }

  #markBatchRequested(batchNumber: number): void {
    if (batchNumber < 0) throw new RangeError('Batch number must be positive');
    if (batchNumber === this.#recentBatchNumbers.curr) return;

    if (this.#recentBatchNumbers.curr !== undefined &&
      Math.abs(this.#recentBatchNumbers.curr - batchNumber) === 1) {
      this.#recentBatchNumbers.prev = this.#recentBatchNumbers.curr;
    }

    this.#recentBatchNumbers.curr = batchNumber;
  }

  #getRelevantBatchesForQuietReset(): number[] {
    if (this.#recentBatchNumbers.curr === undefined) {
      return [0];
    } else if (this.#recentBatchNumbers.prev === undefined) {
      return [this.#recentBatchNumbers.curr];
    } else {
      return [this.#recentBatchNumbers.prev, this.#recentBatchNumbers.curr];
    }
  }

  async #runQuietResetOnce(): Promise<void> {
    const generation = this.#generation;
    const batches = this.#getRelevantBatchesForQuietReset();

    try {
      const [newCount, entries] = await Promise.all([
        this.#countEntries(),
        this.#fetchEntriesForQuietReset(batches),
      ]);

      if (this.#generation !== generation) return; // Stale

      this.#swapCachesForQuietReset(batches, entries, newCount);
    } catch (e) {
      if (this.#generation !== generation) return;
      this.error = e instanceof Error ? e : new Error(String(e));
    }
  }

  async #fetchEntriesForQuietReset(batches: number[]): Promise<IEntry[]> {
    const offset = batches.sort()[0] * this.batchSize;
    return this.#fetchRange(offset, this.batchSize * batches.length);
  }

  #swapCachesForQuietReset(batches: number[], entries: IEntry[], newCount: number): void {
    this.#entryCache.clear();
    this.#idToIndex.clear();
    this.#pendingBatches.clear();
    this.#loadedBatches.clear();

    const offset = batches.sort()[0] * this.batchSize;
    for (let i = 0; i < entries.length; i++) {
      const index = offset + i;
      const entry = entries[i];
      this.#entryCache.set(index, entry);
      this.#idToIndex.set(entry.id, index);
    }

    batches.forEach(batch => this.#loadedBatches.add(batch));
    this.#generation++;
    this.totalCount = newCount;
    this.error = undefined;
  }
}
