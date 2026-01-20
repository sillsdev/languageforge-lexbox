import {afterEach, describe, expect, it, vi} from 'vitest';

import {EntryLoaderService} from '$lib/services/entry-loader-service.svelte';
import type {IEntry} from '$lib/dotnet-types';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IMiniLcmJsInvokable';
import {defaultEntry} from '$lib/utils';

function makeEntry(id: string): IEntry {
  return {
    ...defaultEntry(),
    id,
  };
}

type MiniLcmApiMock = {
  countEntries: ReturnType<typeof vi.fn>;
  getEntries: ReturnType<typeof vi.fn>;
  searchEntries: ReturnType<typeof vi.fn>;
  getEntryIndex: ReturnType<typeof vi.fn>;
};

function createService(entries: IEntry[], totalCount = entries.length) {
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

  return {api, service, cleanup};
}

describe('EntryLoaderService', () => {
  let cleanups: (() => void)[] = [];

  afterEach(() => {
    for (const cleanup of cleanups) cleanup();
    cleanups = [];
  });

  it('loads a batch and caches entries', async () => {
    const entry0 = makeEntry('e0');
    const entry1 = makeEntry('e1');
    const {api, service, cleanup} = createService([entry0, entry1]);
    cleanups.push(cleanup);

    await service.getOrLoadEntryByIndex(1);

    expect(api.getEntries).toHaveBeenCalledTimes(1);
    expect(service.getCachedEntryByIndex(0)?.id).toBe('e0');
    expect(await service.getOrLoadEntryIndex('e1')).toBe(1);
  });

  it('returns cached entries without refetching', async () => {
    const entry0 = makeEntry('e0');
    const entry1 = makeEntry('e1');
    const {api, service, cleanup} = createService([entry0, entry1]);
    cleanups.push(cleanup);

    await service.getOrLoadEntryByIndex(0);
    await service.getOrLoadEntryByIndex(0);

    expect(api.getEntries).toHaveBeenCalledTimes(1);
  });

  it('removes entries and shifts indices', async () => {
    const entry0 = makeEntry('e0');
    const entry1 = makeEntry('e1');
    const entry2 = makeEntry('e2');
    const {service, cleanup} = createService([entry0, entry1, entry2], 3);
    cleanups.push(cleanup);

    await service.loadInitialCount();
    await service.getOrLoadEntryByIndex(2);

    service.removeEntryById('e1');

    expect(await service.getOrLoadEntryIndex('e2')).toBe(1);
    expect(service.getCachedEntryByIndex(1)?.id).toBe('e2');
    expect(service.totalCount).toBe(2);
  });

  it('increments version on update', async () => {
    const entry0 = makeEntry('e0');
    const {service, cleanup} = createService([entry0]);
    cleanups.push(cleanup);

    await service.getOrLoadEntryByIndex(0);
    const v1 = service.getVersion(0);

    await service.updateEntry({...entry0, citationForm: {en: 'updated'}});
    const v2 = service.getVersion(0);

    expect(v1).toBe(1);
    expect(v2).toBe(2);
  });

  it('recalculates loaded batches when totalCount is undefined', async () => {
    const entry0 = makeEntry('e0');
    const entry1 = makeEntry('e1');
    const entry2 = makeEntry('e2');
    const {api, service, cleanup} = createService([entry0, entry1, entry2]);
    cleanups.push(cleanup);

    await service.getOrLoadEntryByIndex(0);
    expect(api.getEntries).toHaveBeenCalledTimes(1);

    service.totalCount = undefined;
    service.removeEntryById('e0');

    await service.getOrLoadEntryByIndex(2);

    expect(api.getEntries).toHaveBeenCalledTimes(2);
  });

  it('inserts entries and shifts indices on update for new entries', async () => {
    const entry0 = makeEntry('e0');
    const entry1 = makeEntry('e1');
    const entryNew = makeEntry('eNew');
    // Initially only e0 and e1
    const {api, service, cleanup} = createService([entry0, entry1], 2);
    cleanups.push(cleanup);

    await service.loadInitialCount();
    await service.getOrLoadEntryByIndex(1); // Load e1 at index 1

    // Pretend eNew is inserted at index 1
    const updatedEntries = [entry0, entryNew, entry1];
    api.getEntryIndex.mockReturnValue((id: string) => {
      const idx = updatedEntries.findIndex(e => e.id === id);
      return Promise.resolve(idx);
    });

    await service.updateEntry(entryNew);

    expect(service.totalCount).toBe(3);
    expect(await service.getOrLoadEntryIndex('eNew')).toBe(1);
    expect(service.getCachedEntryByIndex(1)?.id).toBe('eNew');
    expect(service.getCachedEntryByIndex(2)?.id).toBe('e1'); // Shifted
  });
});
