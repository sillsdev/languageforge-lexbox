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
import {isSenseRowSort, type SortConfig} from '$project/browse/sort/options';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';

interface QueryDeps {
  search: () => string;
  sort: () => SortConfig | undefined;
  gridifyFilter: () => string | undefined;
}

/**
 * A row in the entry list. Gloss sorting produces one row per sense
 * (senseId tells which one); other sorts produce one row per entry.
 */
export interface EntryListRow {
  entry: IEntry;
  senseId?: string;
}

const EVENT_DEBOUNCE_MS = 600;


// useDebounce's types don't account for awaiting async callbacks, so we specify
// void (not Promise<void>) to get the correct runtime return type.
type DebouncedVoidFn = ReturnType<typeof useDebounce<[], void>>;

export class EntryLoaderService {
  static readonly DEFAULT_GENERATION = 0;

  totalCount = $state<number | undefined>();
  loading = $state(true);
  error = $state<Error | undefined>();

  #generation = $state(-1);
  readonly #cache: EntryCache;
  readonly #viewport = new ViewportTracker();

  #debouncedFilterReset = useDebounce(() => this.#executeReset(false), DEFAULT_DEBOUNCE_TIME) as unknown as DebouncedVoidFn;
  #debouncedEventReset = useDebounce(() => this.#executeReset(true), EVENT_DEBOUNCE_MS) as unknown as DebouncedVoidFn;
  #filterResetInFlight?: Promise<void>;
  #eventPendingAfterFilterReset = false;

  constructor(private readonly api: IMiniLcmJsInvokable, private readonly deps: QueryDeps, private readonly batchSize = 50) {
    this.#cache = new EntryCache(batchSize);

    watch(
      () => [deps.search(), deps.sort(), deps.gridifyFilter()],
      (_, prev) => {
        // initial load (inside the watch so we've waited a tick for deps to get set)
        // I don't entirely understand it, but without setTimeout tests hang with "effect_update_depth_exceeded"
        if (!prev) setTimeout(() => void this.#executeReset(false));
        // an update/change in the deps
        else void this.#scheduleFilterReset();
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

  getCachedRowByIndex(index: number): EntryListRow | undefined {
    this.#viewport.markViewed(this.#batchFor(index));
    return this.#cache.getByIndex(index);
  }

  async getOrLoadRowByIndex(index: number): Promise<EntryListRow | undefined> {
    this.#viewport.markViewed(this.#batchFor(index));

    const cached = this.#cache.getByIndex(index);
    if (cached) return cached;

    const gen = this.#generation;
    await this.#loadBatch(this.#batchFor(index));

    return gen === this.#generation ? this.#cache.getByIndex(index) : undefined;
  }

  /** The index of the entry's row, or of its first row when sorting produces a row per sense. */
  async getOrLoadEntryIndex(id: string): Promise<number> {
    const cached = this.#cache.getIndexById(id);
    if (cached !== undefined) return cached;

    const gen = this.#generation;
    const search = this.deps.search() || undefined;
    const options = this.#buildQueryOptions(0, 0);
    const index = await (this.#senseRowMode
      ? this.api.getEntrySenseRowIndex(id, search, options)
      : this.api.getEntryIndex(id, search, options));

    if (gen !== this.#generation) return -1;
    this.#cache.setIndexForId(id, index);
    return index;
  }

  reset(): Promise<void> {
    return this.#scheduleFilterReset();
  }

  quietReset(): Promise<void> {
    if (this.#filterResetInFlight) {
      this.#eventPendingAfterFilterReset = true;
      return this.#filterResetInFlight;
    }
    return this.#scheduleEventReset();
  }

  async #scheduleFilterReset(): Promise<void> {
    this.loading = true;
    this.#debouncedEventReset.cancel();
    this.#eventPendingAfterFilterReset = false;
    // Filter resets take precedence; any entry events that arrive mid-reset are
    // replayed afterward via the eventPendingAfterFilterReset flag to ensure all updates are
    // eventually applied to the shown data.
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

  #scheduleEventReset(): Promise<void> {
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
        this.#cache.storeBatches(batchesToPreload, entries);
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
          // We intentionally only communicate the reset to listeners once both:
          // (1) the new count is in place and (2) there's no stale data left.
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
      this.#cache.storeBatches([batch], entries);
    } catch (e) {
      // Surfacing this on the service triggers the top-level error UI + toast in EntriesList.
      if (gen === this.#generation) {
        this.error = e instanceof Error ? e : new Error(String(e));
      }
      throw e;
    } finally {
      this.#cache.clearPendingBatch(batch, promise);
    }
  }

  get #senseRowMode(): boolean {
    return isSenseRowSort(this.deps.sort());
  }

  #fetchCount(): Promise<number> {
    const search = this.deps.search() || undefined;
    return this.#senseRowMode
      ? this.api.countEntrySenseRows(search, this.#buildFilterOptions())
      : this.api.countEntries(search, this.#buildFilterOptions());
  }

  #fetchBatch(batch: number): Promise<EntryListRow[]> {
    return this.#fetchRange(batch * this.batchSize, this.batchSize);
  }

  #fetchBatchRange(batches: number[]): Promise<EntryListRow[]> {
    const start = Math.min(...batches);
    return this.#fetchRange(start * this.batchSize, batches.length * this.batchSize);
  }

  async #fetchRange(offset: number, count: number): Promise<EntryListRow[]> {
    const search = this.deps.search();
    const options = this.#buildQueryOptions(offset, count);
    if (this.#senseRowMode) {
      const rows = await this.api.getEntrySenseRows(search || undefined, options);
      return rows.map(row => ({entry: row.entry, senseId: row.senseId}));
    }
    const entries = await (search
      ? this.api.searchEntries(search, options)
      : this.api.getEntries(options));
    return entries.map(entry => ({entry}));
  }

  #buildFilterOptions(): IFilterQueryOptions {
    const filter = this.deps.gridifyFilter();
    return { filter: filter ? { gridifyFilter: filter } : undefined };
  }

  #buildQueryOptions(offset: number, count: number): IQueryOptions {
    const sort = this.deps.sort();
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
    return Math.floor(index / this.batchSize);
  }
}

class EntryCache {
  #rows = new SvelteMap<number, EntryListRow>();
  #idToIndex = new SvelteMap<string, number>();
  #pending = new SvelteMap<number, Promise<EntryListRow[]>>();
  #loaded = new SvelteSet<number>();

  constructor(private readonly batchSize: number) {}

  getByIndex(index: number): EntryListRow | undefined {
    return this.#rows.get(index);
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

  getPendingBatch(batch: number): Promise<EntryListRow[]> | undefined {
    return this.#pending.get(batch);
  }

  setPendingBatch(batch: number, promise: Promise<EntryListRow[]>): void {
    this.#pending.set(batch, promise);
  }

  clearPendingBatch(batch: number, promise: Promise<EntryListRow[]>): void {
    if (this.#pending.get(batch) === promise) {
      this.#pending.delete(batch);
    }
  }

  storeBatches(batches: number[], rows: EntryListRow[]): void {
    batches.sort((a, b) => a - b);

    // ensure batches are consecutive
    for (let i = 1; i < batches.length; i++) {
      if (batches[i] !== batches[i - 1] + 1) {
        throw new Error('Batches must be consecutive');
      }
    }

    if (!batches.length) return;

    const startIndex = batches[0] * this.batchSize;
    for (let i = 0; i < rows.length; i++) {
      const index = startIndex + i;
      this.#rows.set(index, rows[i]);
      // a sense row may not be its entry's first row, so only the index
      // API (setIndexForId) is allowed to map its entry id to an index
      if (rows[i].senseId === undefined) {
        this.#idToIndex.set(rows[i].entry.id, index);
      }
    }
    batches.forEach(b => this.#loaded.add(b));
  }

  clear(): void {
    this.#rows.clear();
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
