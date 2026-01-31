/**
 * EntryLoaderService - On-demand entry loading with batch caching.
 *
 * Refactored for clarity with these design principles:
 * 1. Single cache object instead of multiple maps
 * 2. Generation guards to simplify stale-check boilerplate
 * 3. Clear separation: Cache, Fetching, Event Handling
 * 4. Descriptive naming throughout
 */

import {DEFAULT_DEBOUNCE_TIME, delay} from '$lib/utils/time';
import {SvelteMap, SvelteSet} from 'svelte/reactivity';
import type {IEntry} from '$lib/dotnet-types';
import type {IFilterQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IFilterQueryOptions';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import type {IQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
import type {SortConfig} from '$project/browse/sort/options';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';
import {watch} from 'runed';

// ============================================================================
// Types
// ============================================================================

interface QueryDeps {
  search: () => string;
  sort: () => SortConfig | undefined;
  gridifyFilter: () => string | undefined;
}

// ============================================================================
// EntryLoaderService
// ============================================================================

export class EntryLoaderService {
  static readonly DEFAULT_GENERATION = 0;

  // === Public Reactive State ===
  totalCount = $state<number | undefined>();
  loading = $state(true);
  error = $state<Error | undefined>();

  // === Private State ===
  #generation = $state(-1);
  #cache = new EntryCache();
  #viewportBatches = new ViewportBatchTracker();
  #quietResetDebouncer = new QuietResetDebouncer(() => this.#executeQuietReset());
  #filterDebounceTimer?: ReturnType<typeof setTimeout>;

  readonly #api: IMiniLcmJsInvokable;
  readonly #deps: QueryDeps;
  readonly #batchSize: number;

  constructor(api: IMiniLcmJsInvokable, deps: QueryDeps, batchSize = 50) {
    this.#api = api;
    this.#deps = deps;
    this.#batchSize = batchSize;

    void this.reset();

    watch(
      () => [deps.search(), deps.sort(), deps.gridifyFilter()],
      (_, prev) => {
        if (!prev) return; // Skip initial
        this.#debounceReset();
      }
    );
  }

  get generation(): number {
    return this.#generation;
  }

  destroy(): void {
    clearTimeout(this.#filterDebounceTimer);
    this.#generation++;
  }

  // === Public: Entry Access ===

  getCachedEntryByIndex(index: number): IEntry | undefined {
    this.#viewportBatches.markAccessed(this.#batchFor(index));
    return this.#cache.getByIndex(index);
  }

  async getOrLoadEntryByIndex(index: number): Promise<IEntry | undefined> {
    this.#viewportBatches.markAccessed(this.#batchFor(index));

    const cached = this.#cache.getByIndex(index);
    if (cached) return cached;

    const gen = this.#generation;
    await this.#ensureBatchLoaded(this.#batchFor(index));

    return gen === this.#generation ? this.#cache.getByIndex(index) : undefined;
  }

  async getOrLoadEntryIndex(id: string): Promise<number> {
    const cached = this.#cache.getIndexById(id);
    if (cached !== undefined) return cached;

    const gen = this.#generation;
    const index = await this.#api.getEntryIndex(
      id,
      this.#deps.search() || undefined,
      this.#buildQueryOptions(0, 0)
    );

    if (gen !== this.#generation) return -1;
    this.#cache.setIndexForId(id, index);
    return index;
  }

  // === Public: Reset Operations ===

  async reset(): Promise<void> {
    await this.#loadCount();
    this.#cache.clear();
    this.#generation++;
  }

  quietReset(): Promise<void> {
    return this.#quietResetDebouncer.request(this.#generation);
  }

  // === Public: Event Handlers ===

  async onEntryDeleted(_id: string): Promise<void> {
    await this.quietReset();
  }

  async onEntryUpdated(_entry: IEntry): Promise<void> {
    await this.quietReset();
  }

  // For backwards compatibility with tests
  async loadInitialCount(): Promise<void> {
    await this.#loadCount();
  }

  // === Private: Loading ===

  async #loadCount(): Promise<void> {
    this.loading = true;
    this.error = undefined;

    const gen = this.#generation;
    try {
      const count = await this.#fetchCount();
      if (gen !== this.#generation) return;
      this.totalCount = count;
    } catch (e) {
      if (gen !== this.#generation) return;
      this.error = e instanceof Error ? e : new Error(String(e));
      this.totalCount = undefined;
    } finally {
      if (gen === this.#generation) this.loading = false;
    }
  }

  async #ensureBatchLoaded(batch: number): Promise<void> {
    if (this.#cache.isBatchLoaded(batch)) return;

    const pending = this.#cache.getPendingBatch(batch);
    if (pending) {
      await pending;
      return;
    }

    const gen = this.#generation;
    const promise = this.#fetchBatch(batch);
    this.#cache.setPendingBatch(batch, promise);

    try {
      const entries = await promise;
      if (gen !== this.#generation) return;
      this.#cache.storeBatch(batch, entries, this.#batchSize);
    } finally {
      this.#cache.clearPendingBatch(batch, promise);
    }
  }

  async #executeQuietReset(): Promise<void> {
    const gen = this.#generation;
    const batches = this.#viewportBatches.getRelevantBatches();

    try {
      const [count, entries] = await Promise.all([
        this.#fetchCount(),
        this.#fetchBatchRange(batches),
      ]);

      if (gen !== this.#generation) return;

      // Atomic swap: clear old cache, store new data, bump generation
      this.#cache.clear();
      const startIndex = Math.min(...batches) * this.#batchSize;
      this.#cache.storeRange(startIndex, entries, batches, this.#batchSize);
      this.#generation++;
      this.totalCount = count;
      this.error = undefined;
    } catch (e) {
      if (gen !== this.#generation) return;
      this.error = e instanceof Error ? e : new Error(String(e));
    }
  }

  #debounceReset(): void {
    clearTimeout(this.#filterDebounceTimer);
    this.#filterDebounceTimer = setTimeout(() => void this.reset(), DEFAULT_DEBOUNCE_TIME);
  }

  // === Private: API Calls ===

  async #fetchCount(): Promise<number> {
    return this.#api.countEntries(
      this.#deps.search() || undefined,
      this.#buildFilterOptions()
    );
  }

  async #fetchBatch(batch: number): Promise<IEntry[]> {
    return this.#fetchRange(batch * this.#batchSize, this.#batchSize);
  }

  async #fetchBatchRange(batches: number[]): Promise<IEntry[]> {
    const startBatch = Math.min(...batches);
    return this.#fetchRange(startBatch * this.#batchSize, batches.length * this.#batchSize);
  }

  async #fetchRange(offset: number, count: number): Promise<IEntry[]> {
    const options = this.#buildQueryOptions(offset, count);
    const search = this.#deps.search();
    return search
      ? this.#api.searchEntries(search, options)
      : this.#api.getEntries(options);
  }

  #buildFilterOptions(): IFilterQueryOptions {
    const filter = this.#deps.gridifyFilter();
    return { filter: filter ? { gridifyFilter: filter } : undefined };
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

  #batchFor(index: number): number {
    return Math.floor(index / this.#batchSize);
  }
}

// ============================================================================
// EntryCache - Encapsulates all cache state and operations
// ============================================================================

class EntryCache {
  #entries = new SvelteMap<number, IEntry>();
  #idToIndex = new SvelteMap<string, number>();
  #pending = new SvelteMap<number, Promise<IEntry[]>>();
  #loaded = new SvelteSet<number>();

  getByIndex(index: number): IEntry | undefined {
    return this.#entries.get(index);
  }

  getIndexById(id: string): number | undefined {
    return this.#idToIndex.get(id);
  }

  setIndexForId(id: string, index: number): void {
    this.#idToIndex.set(id, index);
  }

  isBatchLoaded(batch: number): boolean {
    return this.#loaded.has(batch);
  }

  getPendingBatch(batch: number): Promise<IEntry[]> | undefined {
    return this.#pending.get(batch);
  }

  setPendingBatch(batch: number, promise: Promise<IEntry[]>): void {
    this.#pending.set(batch, promise);
  }

  clearPendingBatch(batch: number, promise: Promise<IEntry[]>): void {
    // Only clear if it's still the same promise (prevents stale clears)
    if (this.#pending.get(batch) === promise) {
      this.#pending.delete(batch);
    }
  }

  storeBatch(batch: number, entries: IEntry[], batchSize: number): void {
    const startIndex = batch * batchSize;
    for (let i = 0; i < entries.length; i++) {
      this.#entries.set(startIndex + i, entries[i]);
      this.#idToIndex.set(entries[i].id, startIndex + i);
    }
    this.#loaded.add(batch);
  }

  storeRange(startIndex: number, entries: IEntry[], batches: number[], _batchSize: number): void {
    for (let i = 0; i < entries.length; i++) {
      this.#entries.set(startIndex + i, entries[i]);
      this.#idToIndex.set(entries[i].id, startIndex + i);
    }
    batches.forEach(b => this.#loaded.add(b));
  }

  clear(): void {
    this.#entries.clear();
    this.#idToIndex.clear();
    this.#pending.clear();
    this.#loaded.clear();
  }
}

// ============================================================================
// ViewportBatchTracker - Tracks which batches the UI is currently viewing
// ============================================================================

class ViewportBatchTracker {
  #current: number | undefined;
  #previous: number | undefined;

  markAccessed(batch: number): void {
    if (batch < 0) throw new RangeError('Batch must be non-negative');
    if (batch === this.#current) return;

    // Track previous only if adjacent to current
    if (this.#current !== undefined && Math.abs(this.#current - batch) === 1) {
      this.#previous = this.#current;
    } else {
      this.#previous = undefined;
    }

    this.#current = batch;
  }

  getRelevantBatches(): number[] {
    if (this.#current === undefined) return [0];
    if (this.#previous === undefined) return [this.#current];
    return [this.#previous, this.#current].sort((a, b) => a - b);
  }
}

// ============================================================================
// QuietResetDebouncer - Coalesces rapid quiet reset requests
// ============================================================================

class QuietResetDebouncer {
  static readonly DEBOUNCE_MS = 600;

  #pending = false;
  #generation = -1;
  #inFlight?: Promise<void>;

  constructor(private executeReset: () => Promise<void>) {}

  request(currentGeneration: number): Promise<void> {
    this.#pending = true;
    this.#generation = currentGeneration;

    if (this.#inFlight) return this.#inFlight;

    this.#inFlight = this.#run().finally(() => {
      this.#inFlight = undefined;
    });

    return this.#inFlight;
  }

  async #run(): Promise<void> {
    while (this.#pending) {
      this.#pending = false;
      await delay(QuietResetDebouncer.DEBOUNCE_MS);

      // Abort if generation changed (full reset trumps quiet reset)
      if (this.#generation === -1) return;
      if (this.#pending) continue;

      await this.executeReset();
    }
  }
}
