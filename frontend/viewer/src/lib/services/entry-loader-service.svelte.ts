/**
 * EntryLoaderService - On-demand entry loading with batch caching.
 *
 * Architecture:
 * - Single reset pipeline with priority-based coalescing
 * - FILTER resets (search/sort changes) override EVENT resets (entry updates)
 * - Debouncing prevents rapid-fire API calls
 * - Generation tracking invalidates stale async operations
 */

import {DEFAULT_DEBOUNCE_TIME} from '$lib/utils/time';
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

const enum ResetPriority {
  EVENT = 1,   // Entry events - background refresh, no loading indicator
  FILTER = 2,  // Filter changes - shows loading, clears cache
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
  #viewport = new ViewportTracker();
  #resetQueue = new ResetQueue();

  readonly #api: IMiniLcmJsInvokable;
  readonly #deps: QueryDeps;
  readonly #batchSize: number;

  constructor(api: IMiniLcmJsInvokable, deps: QueryDeps, batchSize = 50) {
    this.#api = api;
    this.#deps = deps;
    this.#batchSize = batchSize;

    void this.#scheduleReset(ResetPriority.FILTER);

    watch(
      () => [deps.search(), deps.sort(), deps.gridifyFilter()],
      (_, prev) => {
        if (prev) void this.#scheduleReset(ResetPriority.FILTER);
      }
    );
  }

  get generation(): number {
    return this.#generation;
  }

  destroy(): void {
    this.#resetQueue.cancel();
    this.#generation++;
  }

  // === Public: Entry Access ===

  getCachedEntryByIndex(index: number): IEntry | undefined {
    this.#viewport.markViewed(this.#batchFor(index));
    return this.#cache.getByIndex(index);
  }

  async getOrLoadEntryByIndex(index: number): Promise<IEntry | undefined> {
    this.#viewport.markViewed(this.#batchFor(index));

    const cached = this.#cache.getByIndex(index);
    if (cached) return cached;

    const gen = this.#generation;
    await this.#loadBatch(this.#batchFor(index));

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

  // === Public: Reset Triggers ===

  reset(): Promise<void> {
    this.#generation++;
    this.#cache.clear();
    return this.#scheduleReset(ResetPriority.FILTER);
  }

  quietReset(): Promise<void> {
    return this.#scheduleReset(ResetPriority.EVENT);
  }

  async onEntryDeleted(_id: string): Promise<void> {
    await this.quietReset();
  }

  async onEntryUpdated(_entry: IEntry): Promise<void> {
    await this.quietReset();
  }

  // For backwards compatibility with tests
  async loadInitialCount(): Promise<void> {
    this.loading = true;
    const gen = this.#generation;
    try {
      const count = await this.#fetchCount();
      if (gen !== this.#generation) return;
      this.totalCount = count;
    } catch (e) {
      if (gen !== this.#generation) return;
      this.error = e instanceof Error ? e : new Error(String(e));
    } finally {
      if (gen === this.#generation) {
        this.loading = false;
      }
    }
  }

  // === Private: Reset Pipeline ===

  #scheduleReset(priority: ResetPriority): Promise<void> {
    if (priority === ResetPriority.FILTER) {
      this.loading = true;
    }
    return this.#resetQueue.schedule(priority, () => this.#executeReset(priority));
  }

  async #executeReset(priority: ResetPriority): Promise<void> {
    const gen = this.#generation;
    const batchesToPreload = priority === ResetPriority.EVENT
      ? this.#viewport.getViewedBatches()
      : [];
    let success = false;

    try {
      const [count, entries] = await Promise.all([
        this.#fetchCount(),
        batchesToPreload.length > 0
          ? this.#fetchBatchRange(batchesToPreload)
          : Promise.resolve([]),
      ]);

      if (gen !== this.#generation) return;

      this.#cache.clear();
      if (entries.length > 0) {
        const startIndex = Math.min(...batchesToPreload) * this.#batchSize;
        this.#cache.storeRange(startIndex, entries, batchesToPreload);
      }

      this.totalCount = count;
      this.error = undefined;
      success = true;
    } catch (e) {
      if (gen !== this.#generation) return;
      this.error = e instanceof Error ? e : new Error(String(e));
    } finally {
      if (gen === this.#generation) {
        if (priority === ResetPriority.FILTER) {
          this.loading = false;
        }
        if (success) {
          this.#generation++;
        }
      }
    }
  }

  // === Private: Batch Loading ===

  async #loadBatch(batch: number): Promise<void> {
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

  // === Private: API ===

  #fetchCount(): Promise<number> {
    return this.#api.countEntries(
      this.#deps.search() || undefined,
      this.#buildFilterOptions()
    );
  }

  #fetchBatch(batch: number): Promise<IEntry[]> {
    return this.#fetchRange(batch * this.#batchSize, this.#batchSize);
  }

  #fetchBatchRange(batches: number[]): Promise<IEntry[]> {
    const start = Math.min(...batches);
    return this.#fetchRange(start * this.#batchSize, batches.length * this.#batchSize);
  }

  #fetchRange(offset: number, count: number): Promise<IEntry[]> {
    const search = this.#deps.search();
    const options = this.#buildQueryOptions(offset, count);
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
// ResetQueue - Debounces and coalesces reset requests by priority
// ============================================================================

class ResetQueue {
  static readonly DEBOUNCE_MS: Record<ResetPriority, number> = {
    [ResetPriority.EVENT]: 600,
    [ResetPriority.FILTER]: DEFAULT_DEBOUNCE_TIME,
  };

  #pending: { priority: ResetPriority; execute: () => Promise<void> } | null = null;
  #timer?: ReturnType<typeof setTimeout>;
  #debounceResolvers: (() => void)[] = [];
  #inFlight?: Promise<void>;

  schedule(priority: ResetPriority, execute: () => Promise<void>): Promise<void> {
    if (!this.#pending || priority >= this.#pending.priority) {
      this.#pending = { priority, execute };
      this.#restartDebounce();
    }
    return this.#inFlight ?? this.#startLoop();
  }

  cancel(): void {
    clearTimeout(this.#timer);
    this.#timer = undefined;
    this.#pending = null;
    this.#resolveDebounce();
  }

  #restartDebounce(): void {
    clearTimeout(this.#timer);
    const ms = ResetQueue.DEBOUNCE_MS[this.#pending!.priority];
    this.#timer = setTimeout(() => {
      this.#timer = undefined;
      this.#resolveDebounce();
    }, ms);
  }

  #resolveDebounce(): void {
    const resolvers = this.#debounceResolvers;
    this.#debounceResolvers = [];
    resolvers.forEach(r => r());
  }

  #waitForDebounce(): Promise<void> {
    if (!this.#timer) return Promise.resolve();
    return new Promise(r => this.#debounceResolvers.push(r));
  }

  #startLoop(): Promise<void> {
    if (this.#inFlight) return this.#inFlight;

    this.#inFlight = (async () => {
      while (this.#pending) {
        await this.#waitForDebounce();
        const pending = this.#pending;
        this.#pending = null;
        if (pending) {
          await pending.execute();
        }
      }
    })().finally(() => {
      this.#inFlight = undefined;
    });

    return this.#inFlight;
  }
}

// ============================================================================
// EntryCache - Manages entry storage and batch tracking
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
    if (this.#pending.get(batch) === promise) {
      this.#pending.delete(batch);
    }
  }

  storeBatch(batch: number, entries: IEntry[], batchSize: number): void {
    const start = batch * batchSize;
    for (let i = 0; i < entries.length; i++) {
      this.#entries.set(start + i, entries[i]);
      this.#idToIndex.set(entries[i].id, start + i);
    }
    this.#loaded.add(batch);
  }

  storeRange(startIndex: number, entries: IEntry[], batches: number[]): void {
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
// ViewportTracker - Tracks which batches the user is currently viewing
// ============================================================================

class ViewportTracker {
  #current: number | undefined;
  #adjacent: number | undefined;

  markViewed(batch: number): void {
    if (batch < 0) throw new RangeError('Batch must be non-negative');
    if (batch === this.#current) return;

    this.#adjacent = this.#current !== undefined && Math.abs(this.#current - batch) === 1
      ? this.#current
      : undefined;
    this.#current = batch;
  }

  getViewedBatches(): number[] {
    if (this.#current === undefined) return [0];
    if (this.#adjacent === undefined) return [this.#current];
    return [this.#adjacent, this.#current].sort((a, b) => a - b);
  }
}
