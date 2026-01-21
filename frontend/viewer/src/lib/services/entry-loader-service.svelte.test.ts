import {afterEach, describe, expect, it, vi} from 'vitest';

import {EntryLoaderService} from '$lib/services/entry-loader-service.svelte';
import type {IEntry} from '$lib/dotnet-types';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {defaultEntry} from '$lib/utils';

// Must match EntryLoaderService.batchSize
const BATCH_SIZE = 50;

function makeEntry(id: string, citation?: string): IEntry {
  return {
    ...defaultEntry(),
    id,
    citationForm: citation ? {en: citation} : defaultEntry().citationForm,
  };
}

function makeEntries(total: number, prefix = 'e'): IEntry[] {
  return Array.from({length: total}, (_, i) => makeEntry(`${prefix}${i}`, `${prefix}${i}`));
}

type MiniLcmApiMock = {
  countEntries: ReturnType<typeof vi.fn>;
  getEntries: ReturnType<typeof vi.fn>;
  searchEntries: ReturnType<typeof vi.fn>;
  getEntryIndex: ReturnType<typeof vi.fn>;
};

async function createService(allEntries: IEntry[], totalCount = allEntries.length) {
  const api: MiniLcmApiMock = {
    countEntries: vi.fn().mockResolvedValue(totalCount),
    getEntries: vi.fn().mockImplementation((options: {offset: number; count: number}) => {
      return Promise.resolve(allEntries.slice(options.offset, options.offset + options.count));
    }),
    searchEntries: vi.fn().mockImplementation((_query: string, options: {offset: number; count: number}) => {
      return Promise.resolve(allEntries.slice(options.offset, options.offset + options.count));
    }),
    getEntryIndex: vi.fn().mockImplementation((id: string) => {
      return Promise.resolve(allEntries.findIndex(e => e.id === id));
    }),
  };

  let service!: EntryLoaderService;
  const cleanup = $effect.root(() => {
    service = new EntryLoaderService(api as unknown as IMiniLcmJsInvokable, {
      search: () => '',
      sort: () => undefined,
      gridifyFilter: () => undefined,
    });
  });

  // Wait for initial load triggered by constructor's watch
  await vi.waitFor(() => expect(service.loading).toBe(false));

  return {api, service, cleanup};
}

describe('EntryLoaderService', () => {
  let cleanups: (() => void)[] = [];

  afterEach(() => {
    for (const cleanup of cleanups) cleanup();
    cleanups = [];
  });

  describe('loading and caching', () => {
    it('loads a batch and caches entries', async () => {
      const entries = makeEntries(3);
      const {api, service, cleanup} = await createService(entries);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(1);

      expect(api.getEntries).toHaveBeenCalledTimes(1);
      expect(service.getCachedEntryByIndex(0)?.id).toBe('e0');
      expect(await service.getOrLoadEntryIndex('e1')).toBe(1);
    });

    it('returns cached entries without refetching', async () => {
      const entries = makeEntries(5);
      const {api, service, cleanup} = await createService(entries);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(0);
      await service.getOrLoadEntryByIndex(0);

      expect(api.getEntries).toHaveBeenCalledTimes(1);
    });

    it('deduplicates concurrent requests for the same batch', async () => {
      const entries = makeEntries(10);
      const {api, service, cleanup} = await createService(entries, entries.length);
      cleanups.push(cleanup);

      const promises = [
        service.getOrLoadEntryByIndex(0),
        service.getOrLoadEntryByIndex(0),
        service.getOrLoadEntryByIndex(0),
      ];

      await Promise.all(promises);

      expect(api.getEntries).toHaveBeenCalledTimes(1);
    });
  });

  describe('quiet reset', () => {
    it('loads two adjacent batches in a single request', async () => {
      const entries = makeEntries(4 * BATCH_SIZE);
      const {api, service, cleanup} = await createService(entries, entries.length);
      cleanups.push(cleanup);

      // Mark batches 0 and 1 as "relevant" (adjacent)
      service.getCachedEntryByIndex(0);
      service.getCachedEntryByIndex(BATCH_SIZE);

      await service.quietReset();

      expect(api.countEntries).toHaveBeenCalled();
      expect(api.getEntries).toHaveBeenCalledWith(expect.objectContaining({offset: 0, count: 2 * BATCH_SIZE}));
    });

    it('loads only the most recent batch when last two are non-adjacent', async () => {
      const entries = makeEntries(10 * BATCH_SIZE);
      const {api, service, cleanup} = await createService(entries, entries.length);
      cleanups.push(cleanup);

      service.getCachedEntryByIndex(0);              // batch 0
      service.getCachedEntryByIndex(3 * BATCH_SIZE); // batch 3

      await service.quietReset();

      expect(api.getEntries).toHaveBeenCalledWith(expect.objectContaining({offset: 3 * BATCH_SIZE, count: BATCH_SIZE}));
    });

    it('does not clear cache before swap (no flicker)', async () => {
      const entries = makeEntries(2 * BATCH_SIZE, 'old');
      const {api, service, cleanup} = await createService(entries, entries.length);
      cleanups.push(cleanup);

      // Warm cache with batch 0
      await service.getOrLoadEntryByIndex(0);
      expect(service.getCachedEntryByIndex(0)?.id).toBe('old0');
      const genBefore = service.generation;
      expect(service.loading).toBe(false);

      // Prepare delayed quiet reset response
      let resolveEntries: (value: IEntry[]) => void;
      let resolveCount: (value: number) => void;
      api.getEntries.mockReturnValueOnce(new Promise<IEntry[]>(r => { resolveEntries = r; }));
      api.countEntries.mockReturnValueOnce(new Promise<number>(r => { resolveCount = r; }));

      // Mark batch 0 as relevant
      service.getCachedEntryByIndex(0);

      const quietResetPromise = service.quietReset();

      // Cache remains available during quiet reset
      expect(service.getCachedEntryByIndex(0)?.id).toBe('old0');
      expect(service.loading).toBe(false);

      // Resolve with new entries
      const newEntries = makeEntries(BATCH_SIZE, 'new');
      resolveCount!(entries.length + 1);
      resolveEntries!(newEntries);
      await quietResetPromise;

      expect(service.generation).toBeGreaterThan(genBefore);
      expect(service.totalCount).toBe(entries.length + 1);
      expect(service.getCachedEntryByIndex(0)?.id).toBe('new0');
    });

    it('debounces multiple calls', async () => {
      const entries = makeEntries(BATCH_SIZE);
      const {api, service, cleanup} = await createService(entries, entries.length);
      cleanups.push(cleanup);

      // Warm batch 0
      await service.getOrLoadEntryByIndex(0);
      const countBefore = api.countEntries.mock.calls.length;

      // Multiple rapid calls
      const p1 = service.quietReset();
      const p2 = service.quietReset();
      const p3 = service.quietReset();

      expect(p1).toBe(p2);
      expect(p2).toBe(p3);

      await p3;

      // Should only have called the API once (after the debounce delay)
      expect(api.countEntries.mock.calls.length).toBe(countBefore + 1);
    });
  });

  describe('race conditions', () => {
    it('discards stale count when reset occurs during loadInitialCount', async () => {
      const entries = makeEntries(200);
      const {api, service, cleanup} = await createService(entries, 100);
      cleanups.push(cleanup);

      let resolveCount: (value: number) => void;
      api.countEntries.mockReturnValueOnce(new Promise<number>(r => { resolveCount = r; }));

      const loadPromise = service.loadInitialCount();

      service.reset();
      api.countEntries.mockResolvedValue(5);
      void service.loadInitialCount();

      resolveCount!(100);
      await loadPromise;

      await vi.waitFor(() => expect(service.loading).toBe(false));
      expect(service.totalCount).toBe(5);
    });

    it('discards stale batch when reset occurs during load', async () => {
      const entries = makeEntries(BATCH_SIZE, 'a');
      const {api, service, cleanup} = await createService(entries, entries.length);
      cleanups.push(cleanup);

      let resolveEntries: (value: IEntry[]) => void;
      api.getEntries.mockReturnValueOnce(new Promise<IEntry[]>(r => { resolveEntries = r; }));

      const loadPromise = service.getOrLoadEntryByIndex(0);

      service.reset();
      const nextEntries = makeEntries(BATCH_SIZE, 'b');
      api.getEntries.mockResolvedValue(nextEntries);
      api.countEntries.mockResolvedValue(nextEntries.length);
      await service.loadInitialCount();

      resolveEntries!(entries.slice(0, BATCH_SIZE));
      const result = await loadPromise;

      expect(result).toBeUndefined();

      const freshResult = await service.getOrLoadEntryByIndex(0);
      expect(freshResult?.id).toBe('b0');
    });

    it('does not allow a stale load to clear a newer pending promise', async () => {
      const entries = makeEntries(2 * BATCH_SIZE, 'v');
      const {api, service, cleanup} = await createService(entries, entries.length);
      cleanups.push(cleanup);

      let resolveFirst: (value: IEntry[]) => void;
      api.getEntries.mockReturnValueOnce(new Promise<IEntry[]>(r => { resolveFirst = r; }));

      const firstLoad = service.getOrLoadEntryByIndex(0);

      // Reset and immediately start another load for the same batch
      service.reset();
      api.countEntries.mockResolvedValue(entries.length);
      await service.loadInitialCount();

      let resolveSecond: (value: IEntry[]) => void;
      api.getEntries.mockReturnValueOnce(new Promise<IEntry[]>(r => { resolveSecond = r; }));
      const secondLoad = service.getOrLoadEntryByIndex(0);

      expect(api.getEntries).toHaveBeenCalledTimes(2);

      // Now resolve the stale (first) load. It should not clobber the newer pending tracking.
      resolveFirst!(entries.slice(0, BATCH_SIZE));

      // If the stale load incorrectly deletes the NEW pending promise, this will trigger a 3rd fetch.
      const thirdLoad = service.getOrLoadEntryByIndex(0);
      await vi.waitFor(() => expect(api.getEntries).toHaveBeenCalledTimes(2));

      resolveSecond!(entries.slice(0, BATCH_SIZE));

      const results = await Promise.all([firstLoad, secondLoad, thirdLoad]);
      // The first load may return undefined or the fresh cached value, depending on timing.
      expect(results[1]?.id).toBe('v0');
      expect(results[2]?.id).toBe('v0');
    });
  });
});
