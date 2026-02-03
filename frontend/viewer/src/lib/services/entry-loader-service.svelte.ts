/**
 * EntryLoaderService - On-demand entry loading with batch caching.
 * @see {@link ./ENTRY_LOADER_SERVICE.md} for reset flow diagram.
 */

import {SvelteMap, SvelteSet} from 'svelte/reactivity';
import {useDebounce, watch} from 'runed';

import {DEFAULT_DEBOUNCE_TIME} from '$lib/utils/time';
import type {IEntry} from '$lib/dotnet-types';
import type {IFilterQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IFilterQueryOptions';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import type {IQueryOptions} from '$lib/dotnet-types/generated-types/MiniLcm/IQueryOptions';
import type {SortConfig} from '$project/browse/sort/options';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';

interface QueryDeps {
  search: () => string;
  sort: () => SortConfig | undefined;
  gridifyFilter: () => string | undefined;
}

const EVENT_DEBOUNCE_MS = 600;

export class EntryLoaderService {
  static readonly DEFAULT_GENERATION = 0;

  totalCount = $state<number | undefined>();
  loading = $state(true);
  error = $state<Error | undefined>();

  #generation = $state(-1);
  #cache = new EntryCache();
  #viewport = new ViewportTracker();

  #debouncedFilterReset = useDebounce(() => this.#executeReset(false), DEFAULT_DEBOUNCE_TIME);
  #debouncedEventReset = useDebounce(() => this.#executeReset(true), EVENT_DEBOUNCE_MS);
  #filterResetInFlight?: Promise<Promise<void>>;
  #eventPendingAfterFilterReset = false;

  readonly #api: IMiniLcmJsInvokable;
  readonly #deps: QueryDeps;
  readonly #batchSize: number;

  constructor(api: IMiniLcmJsInvokable, deps: QueryDeps, batchSize = 50) {
    this.#api = api;
    this.#deps = deps;
    this.#batchSize = batchSize;

    void this.#executeReset(false);

    watch(
      () => [deps.search(), deps.sort(), deps.gridifyFilter()],
      (_, prev) => {
        if (prev) void this.#scheduleFilterReset();
      }
    );
  }

  get generation(): number {
    return this.#generation;
  }

  destroy(): void {
    this.#debouncedFilterReset.cancel();
    this.#debouncedEventReset.cancel();
    this.#filterResetInFlight = undefined;
    this.#eventPendingAfterFilterReset = false;
    this.#generation++;
  }

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

  reset(): Promise<void> {
    return this.#scheduleFilterReset();
  }

  async quietReset(): Promise<void> {
    if (this.#filterResetInFlight) {
      this.#eventPendingAfterFilterReset = true;
      return this.#filterResetInFlight;
    }
    return this.#scheduleEventReset();
  }

  async onEntryDeleted(_id: string): Promise<void> {
    await this.quietReset();
  }

  async onEntryUpdated(_entry: IEntry): Promise<void> {
    await this.quietReset();
  }

  async #scheduleFilterReset(): Promise<void> {
    this.loading = true;
    this.#debouncedEventReset.cancel();
    this.#eventPendingAfterFilterReset = false;
    const filterResetPromise = this.#debouncedFilterReset();
    this.#filterResetInFlight = filterResetPromise;
    await this.#filterResetInFlight;
    if (this.#filterResetInFlight === filterResetPromise) {
      this.#filterResetInFlight = undefined;
      if (this.#eventPendingAfterFilterReset) {
        this.#eventPendingAfterFilterReset = false;
        await this.#scheduleEventReset();
      }
    }
  }

  async #scheduleEventReset(): Promise<void> {
    return this.#debouncedEventReset();
  }

  async #executeReset(isQuiet: boolean): Promise<void> {
    const gen = this.#generation;
    const batchesToPreload = isQuiet ? this.#viewport.getViewedBatches() : [];
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
        if (!isQuiet) {
          this.loading = false;
        }
        if (success) {
          this.#generation++;
        }
      }
    }
  }

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
