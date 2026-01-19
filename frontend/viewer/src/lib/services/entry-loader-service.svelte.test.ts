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
};

function createService(entries: IEntry[], totalCount = entries.length) {
  const api: MiniLcmApiMock = {
    countEntries: vi.fn().mockResolvedValue(totalCount),
    getEntries: vi.fn().mockResolvedValue(entries),
    searchEntries: vi.fn().mockResolvedValue(entries),
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

    await service.loadEntryByIndex(1);

    expect(api.getEntries).toHaveBeenCalledTimes(1);
    expect(service.getEntryByIndex(0)?.id).toBe('e0');
    expect(service.getIndexById('e1')).toBe(1);
  });

  it('returns cached entries without refetching', async () => {
    const entry0 = makeEntry('e0');
    const entry1 = makeEntry('e1');
    const {api, service, cleanup} = createService([entry0, entry1]);
    cleanups.push(cleanup);

    await service.loadEntryByIndex(0);
    await service.loadEntryByIndex(0);

    expect(api.getEntries).toHaveBeenCalledTimes(1);
  });

  it('removes entries and shifts indices', async () => {
    const entry0 = makeEntry('e0');
    const entry1 = makeEntry('e1');
    const entry2 = makeEntry('e2');
    const {service, cleanup} = createService([entry0, entry1, entry2], 3);
    cleanups.push(cleanup);

    await service.loadCount();
    await service.loadEntryByIndex(2);

    service.removeEntryById('e1');

    expect(service.getIndexById('e2')).toBe(1);
    expect(service.getEntryByIndex(1)?.id).toBe('e2');
    expect(service.totalCount).toBe(2);
  });

  it('recalculates loaded batches when totalCount is undefined', async () => {
    const entry0 = makeEntry('e0');
    const entry1 = makeEntry('e1');
    const entry2 = makeEntry('e2');
    const {api, service, cleanup} = createService([entry0, entry1, entry2]);
    cleanups.push(cleanup);

    await service.loadEntryByIndex(0);
    expect(api.getEntries).toHaveBeenCalledTimes(1);

    service.totalCount = undefined;
    service.removeEntryById('e0');

    await service.loadEntryByIndex(2);

    expect(api.getEntries).toHaveBeenCalledTimes(2);
  });
});
