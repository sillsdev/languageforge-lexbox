import {afterEach, describe, expect, it, vi} from 'vitest';

import {EntryLoaderService} from '$lib/services/entry-loader-service.svelte';
import type {IEntry} from '$lib/dotnet-types';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {defaultEntry} from '$lib/utils';

// Must match EntryLoaderService.batchSize
const BATCH_SIZE = 50;

function makeEntry(id: string): IEntry {
  return {
    ...defaultEntry(),
    id,
  };
}

function makeBatch(startId: number, count = BATCH_SIZE): IEntry[] {
  return Array.from({length: count}, (_, i) => makeEntry(`e${startId + i}`));
}

type MiniLcmApiMock = {
  countEntries: ReturnType<typeof vi.fn>;
  getEntries: ReturnType<typeof vi.fn>;
  searchEntries: ReturnType<typeof vi.fn>;
  getEntryIndex: ReturnType<typeof vi.fn>;
};

async function createService(entries: IEntry[], totalCount = entries.length) {
  const api: MiniLcmApiMock = {
    countEntries: vi.fn().mockResolvedValue(totalCount),
    getEntries: vi.fn().mockResolvedValue(entries),
    searchEntries: vi.fn().mockResolvedValue(entries),
    getEntryIndex: vi.fn().mockImplementation((id: string) => {
      return Promise.resolve(entries.findIndex(e => e.id === id));
    }),
  };

  let service!: EntryLoaderService;
  const cleanup = $effect.root(() => {
    service = new EntryLoaderService({
      miniLcmApi: () => api as unknown as IMiniLcmJsInvokable,
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
      const entry0 = makeEntry('e0');
      const entry1 = makeEntry('e1');
      const {api, service, cleanup} = await createService([entry0, entry1]);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(1);

      expect(api.getEntries).toHaveBeenCalledTimes(1);
      expect(service.getCachedEntryByIndex(0)?.id).toBe('e0');
      expect(await service.getOrLoadEntryIndex('e1')).toBe(1);
    });

    it('returns cached entries without refetching', async () => {
      const entry0 = makeEntry('e0');
      const entry1 = makeEntry('e1');
      const {api, service, cleanup} = await createService([entry0, entry1]);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(0);
      await service.getOrLoadEntryByIndex(0);

      expect(api.getEntries).toHaveBeenCalledTimes(1);
    });

    it('deduplicates concurrent requests for the same batch', async () => {
      const entry0 = makeEntry('e0');
      const {api, service, cleanup} = await createService([entry0], 1);
      cleanups.push(cleanup);

      const promises = [
        service.getOrLoadEntryByIndex(0),
        service.getOrLoadEntryByIndex(0),
        service.getOrLoadEntryByIndex(0),
      ];

      await Promise.all(promises);

      expect(api.getEntries).toHaveBeenCalledTimes(1);
    });

    it('reloads batches when totalCount becomes undefined', async () => {
      const entry0 = makeEntry('e0');
      const entry1 = makeEntry('e1');
      const entry2 = makeEntry('e2');
      const {api, service, cleanup} = await createService([entry0, entry1, entry2]);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(0);
      expect(api.getEntries).toHaveBeenCalledTimes(1);

      service.totalCount = undefined;
      service.removeEntryById('e0');

      await service.getOrLoadEntryByIndex(2);

      expect(api.getEntries).toHaveBeenCalledTimes(2);
    });
  });

  describe('update operations', () => {
    it('increments version on update', async () => {
      const entry0 = makeEntry('e0');
      const {service, cleanup} = await createService([entry0]);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(0);
      const v1 = service.getVersion(0);

      await service.updateEntry({...entry0, citationForm: {en: 'updated'}});
      const v2 = service.getVersion(0);

      expect(v1).toBe(1);
      expect(v2).toBe(2);
    });
  });

  describe('insert operations', () => {
    it('inserts entry and shifts subsequent cached entries', async () => {
      const entry0 = makeEntry('e0');
      const entry1 = makeEntry('e1');
      const entryNew = makeEntry('eNew');
      const {api, service, cleanup} = await createService([entry0, entry1], 2);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(1);

      api.getEntryIndex.mockResolvedValue(1);
      api.countEntries.mockResolvedValue(3);

      await service.updateEntry(entryNew);

      expect(service.totalCount).toBe(3);
      expect(service.getCachedEntryByIndex(1)?.id).toBe('eNew');
      expect(service.getCachedEntryByIndex(2)?.id).toBe('e1');
    });

    it('insert beyond loaded range only updates totalCount', async () => {
      const entry0 = makeEntry('e0');
      const entryNew = makeEntry('eNew');
      const {api, service, cleanup} = await createService([entry0], 1);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(0);

      api.getEntryIndex.mockResolvedValue(1);
      api.countEntries.mockResolvedValue(2);

      await service.updateEntry(entryNew);

      expect(service.totalCount).toBe(2);
      expect(service.getCachedEntryByIndex(1)).toBeUndefined();
      expect(service.getCachedEntryByIndex(0)?.id).toBe('e0');
    });

    it('insert before loaded range shifts entries without caching new entry', async () => {
      const batch1 = makeBatch(BATCH_SIZE); // e50-e99
      const {api, service, cleanup} = await createService(batch1, BATCH_SIZE);
      cleanups.push(cleanup);

      api.getEntries.mockResolvedValue(batch1);
      await service.getOrLoadEntryByIndex(BATCH_SIZE);

      expect(service.getCachedEntryByIndex(BATCH_SIZE)?.id).toBe('e50');

      const entryNew = makeEntry('eNew');
      api.getEntryIndex.mockResolvedValue(10);
      api.countEntries.mockResolvedValue(BATCH_SIZE + 1);

      await service.updateEntry(entryNew);

      expect(service.totalCount).toBe(BATCH_SIZE + 1);
      expect(service.getCachedEntryByIndex(BATCH_SIZE + 1)?.id).toBe('e50');
      expect(service.getCachedEntryByIndex(10)).toBeUndefined();
    });

    it('insert in cache hole caches entry and shifts subsequent', async () => {
      const batch0 = makeBatch(0);                  // e0-e49
      const batch2 = makeBatch(2 * BATCH_SIZE);     // e100-e149
      const {api, service, cleanup} = await createService([...batch0, ...batch2], 3 * BATCH_SIZE);
      cleanups.push(cleanup);

      api.getEntries.mockResolvedValueOnce(batch0);
      await service.getOrLoadEntryByIndex(0);

      api.getEntries.mockResolvedValueOnce(batch2);
      await service.getOrLoadEntryByIndex(100);

      expect(service.getCachedEntryByIndex(50)).toBeUndefined(); // Hole

      const entryNew = makeEntry('eNew');
      api.getEntryIndex.mockResolvedValue(75);
      api.countEntries.mockResolvedValue(151);

      await service.updateEntry(entryNew);

      expect(service.totalCount).toBe(151);
      expect(service.getCachedEntryByIndex(0)?.id).toBe('e0');
      expect(service.getCachedEntryByIndex(49)?.id).toBe('e49');
      expect(service.getCachedEntryByIndex(75)?.id).toBe('eNew');
      expect(service.getCachedEntryByIndex(101)?.id).toBe('e100');
    });

    it('insert at index 0 shifts all cached entries', async () => {
      const batch0 = makeBatch(0);
      const {api, service, cleanup} = await createService(batch0, BATCH_SIZE);
      cleanups.push(cleanup);

      api.getEntries.mockResolvedValueOnce(batch0);
      await service.getOrLoadEntryByIndex(0);

      const entryNew = makeEntry('eNew');
      api.getEntryIndex.mockResolvedValue(0);
      api.countEntries.mockResolvedValue(BATCH_SIZE + 1);

      await service.updateEntry(entryNew);

      expect(service.totalCount).toBe(BATCH_SIZE + 1);
      expect(service.getCachedEntryByIndex(0)?.id).toBe('eNew');
      expect(service.getCachedEntryByIndex(1)?.id).toBe('e0');
      expect(service.getCachedEntryByIndex(BATCH_SIZE)?.id).toBe('e49');
    });

    it('insert at batch boundary shifts entire subsequent batch', async () => {
      const batch1 = makeBatch(BATCH_SIZE); // e50-e99
      const {api, service, cleanup} = await createService(batch1, 2 * BATCH_SIZE);
      cleanups.push(cleanup);

      api.getEntries.mockResolvedValueOnce(batch1);
      await service.getOrLoadEntryByIndex(BATCH_SIZE);

      const entryNew = makeEntry('eNew');
      api.getEntryIndex.mockResolvedValue(BATCH_SIZE);
      api.countEntries.mockResolvedValue(2 * BATCH_SIZE + 1);

      await service.updateEntry(entryNew);

      expect(service.totalCount).toBe(2 * BATCH_SIZE + 1);
      expect(service.getCachedEntryByIndex(BATCH_SIZE)?.id).toBe('eNew');
      expect(service.getCachedEntryByIndex(BATCH_SIZE + 1)?.id).toBe('e50');
      expect(service.getCachedEntryByIndex(2 * BATCH_SIZE)?.id).toBe('e99');
    });

    it('insert at last index of batch shifts entry to next batch', async () => {
      const batch0 = makeBatch(0);
      const {api, service, cleanup} = await createService(batch0, BATCH_SIZE);
      cleanups.push(cleanup);

      api.getEntries.mockResolvedValueOnce(batch0);
      await service.getOrLoadEntryByIndex(0);

      const entryNew = makeEntry('eNew');
      api.getEntryIndex.mockResolvedValue(BATCH_SIZE - 1);
      api.countEntries.mockResolvedValue(BATCH_SIZE + 1);

      await service.updateEntry(entryNew);

      expect(service.totalCount).toBe(BATCH_SIZE + 1);
      expect(service.getCachedEntryByIndex(BATCH_SIZE - 2)?.id).toBe('e48');
      expect(service.getCachedEntryByIndex(BATCH_SIZE - 1)?.id).toBe('eNew');
      expect(service.getCachedEntryByIndex(BATCH_SIZE)?.id).toBe('e49');
    });
  });

  describe('delete operations', () => {
    it('removes entry and shifts subsequent cached entries', async () => {
      const entry0 = makeEntry('e0');
      const entry1 = makeEntry('e1');
      const entry2 = makeEntry('e2');
      const {service, cleanup} = await createService([entry0, entry1, entry2], 3);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(2);

      service.removeEntryById('e1');

      expect(await service.getOrLoadEntryIndex('e2')).toBe(1);
      expect(service.getCachedEntryByIndex(1)?.id).toBe('e2');
      expect(service.totalCount).toBe(2);
    });

    it('delete of uncached entry triggers reset', async () => {
      const entry0 = makeEntry('e0');
      const entry1 = makeEntry('e1');
      const {api, service, cleanup} = await createService([entry0, entry1], 2);
      cleanups.push(cleanup);

      const initialCallCount = api.countEntries.mock.calls.length;

      service.removeEntryById('e1');

      await vi.waitFor(() => expect(api.countEntries.mock.calls.length).toBe(initialCallCount + 1));
    });

    it('delete at first index shifts all entries down', async () => {
      const batch0 = makeBatch(0);
      const {service, cleanup} = await createService(batch0, BATCH_SIZE);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(0);

      service.removeEntryById('e0');

      expect(service.totalCount).toBe(BATCH_SIZE - 1);
      expect(service.getCachedEntryByIndex(0)?.id).toBe('e1');
      expect(service.getCachedEntryByIndex(BATCH_SIZE - 2)?.id).toBe('e49');
    });

    it('delete at last cached index only removes that entry', async () => {
      const batch0 = makeBatch(0);
      const {service, cleanup} = await createService(batch0, BATCH_SIZE);
      cleanups.push(cleanup);

      await service.getOrLoadEntryByIndex(0);

      service.removeEntryById('e49');

      expect(service.totalCount).toBe(BATCH_SIZE - 1);
      expect(service.getCachedEntryByIndex(BATCH_SIZE - 2)?.id).toBe('e48');
      expect(service.getCachedEntryByIndex(BATCH_SIZE - 1)).toBeUndefined();
    });

    it('delete at batch boundary shifts entries across batches', async () => {
      const batch0 = makeBatch(0);
      const batch1 = makeBatch(BATCH_SIZE);
      const {api, service, cleanup} = await createService([...batch0, ...batch1], 2 * BATCH_SIZE);
      cleanups.push(cleanup);

      api.getEntries.mockResolvedValueOnce(batch0);
      await service.getOrLoadEntryByIndex(0);
      api.getEntries.mockResolvedValueOnce(batch1);
      await service.getOrLoadEntryByIndex(BATCH_SIZE);

      service.removeEntryById('e49');

      expect(service.totalCount).toBe(2 * BATCH_SIZE - 1);
      expect(service.getCachedEntryByIndex(BATCH_SIZE - 2)?.id).toBe('e48');
      expect(service.getCachedEntryByIndex(BATCH_SIZE - 1)?.id).toBe('e50');
      expect(service.getCachedEntryByIndex(2 * BATCH_SIZE - 2)?.id).toBe('e99');
    });
  });

  describe('race conditions', () => {
    it('discards stale count when reset occurs during loadInitialCount', async () => {
      const entry0 = makeEntry('e0');
      const {api, service, cleanup} = await createService([entry0], 100);
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
      const entry0 = makeEntry('e0');
      const entry1 = makeEntry('e1');
      const {api, service, cleanup} = await createService([entry0], 1);
      cleanups.push(cleanup);

      let resolveEntries: (value: IEntry[]) => void;
      api.getEntries.mockReturnValueOnce(new Promise<IEntry[]>(r => { resolveEntries = r; }));

      const loadPromise = service.getOrLoadEntryByIndex(0);

      service.reset();
      api.getEntries.mockResolvedValue([entry1]);
      api.countEntries.mockResolvedValue(1);
      await service.loadInitialCount();

      resolveEntries!([entry0]);
      const result = await loadPromise;

      expect(result).toBeUndefined();

      const freshResult = await service.getOrLoadEntryByIndex(0);
      expect(freshResult?.id).toBe('e1');
    });
  });
});
