import {beforeEach, describe, expect, it, vi} from 'vitest';
import {PluginApiAdapter, type PluginHostCallbacks, MAX_SAVED_BYTES_PER_RUN, MAX_SAVED_FILES_PER_RUN} from './plugin-api-adapter';
import {PluginApiException, type PluginWriteOperation} from './plugin-api-types';
import {createMemoryStorageBackend, PluginStorage} from './plugin-local-data';
import type {IEntry, IMiniLcmJsInvokable} from '$lib/dotnet-types';

const ENTRY_ID = '11111111-1111-4111-8111-111111111111';

function makeEntry(overrides: Partial<IEntry> = {}): IEntry {
  return {
    id: ENTRY_ID,
    lexemeForm: {seh: 'nyumba'},
    citationForm: {},
    literalMeaning: {},
    note: {},
    senses: [],
    components: [],
    complexForms: [],
    complexFormTypes: [],
    publishIn: [],
    morphType: 'Stem',
    homographNumber: 0,
    ...overrides,
  } as unknown as IEntry;
}

// Kept un-cast so assertions read vi.fn properties directly (the adapter casts when consuming).
function makeApi(overrides: Partial<Record<string, unknown>> = {}) {
  return {
    getWritingSystems: vi.fn(() => Promise.resolve({vernacular: [{wsId: 'seh', isAudio: false}], analysis: []})),
    getMorphTypes: vi.fn(() => Promise.resolve([])),
    getEntry: vi.fn(() => Promise.resolve<IEntry | null>(makeEntry())),
    createEntry: vi.fn((entry: IEntry) => Promise.resolve(entry)),
    updateEntry: vi.fn((_before: IEntry, after: IEntry) => Promise.resolve(after)),
    ...overrides,
  };
}
type MockApi = ReturnType<typeof makeApi>;

function makeAdapter(options: {
  api?: MockApi;
  permissions?: ('edit' | 'internet')[];
  capabilities?: {comments: boolean; history: boolean};
  confirmWrite?: (operation: PluginWriteOperation) => Promise<boolean>;
  writeMode?: 'live' | 'preview';
  storage?: PluginStorage;
} = {}) {
  const confirmWrite = options.confirmWrite ?? (() => Promise.resolve(true));
  const callbacks: PluginHostCallbacks = {confirmWrite, openEntry: vi.fn(), notify: vi.fn()};
  return new PluginApiAdapter(
    (options.api ?? makeApi()) as unknown as IMiniLcmJsInvokable,
    options.storage ?? new PluginStorage('test-project', 'test-plugin'),
    {
      permissions: options.permissions ?? ['edit'],
      capabilities: options.capabilities ?? {comments: true, history: true},
      writeMode: options.writeMode,
    },
    callbacks,
  );
}

beforeEach(() => localStorage.clear());

describe('handle dispatch', () => {
  it('rejects unknown methods', async () => {
    const adapter = makeAdapter();
    await expect(async () => await adapter.handle('dropAllTables', [])).rejects.toMatchObject({code: 'unknown-method'});
  });

  it('rejects malformed args via the decoder table', async () => {
    const adapter = makeAdapter();
    await expect(async () => await adapter.handle('getEntry', ['not-a-guid'])).rejects.toMatchObject({code: 'invalid-args'});
    await expect(async () => await adapter.handle('notify', [42])).rejects.toMatchObject({code: 'invalid-args'});
  });

  it('dispatches decoded calls to the typed implementation', async () => {
    const api = makeApi();
    const adapter = makeAdapter({api});
    const entry = await adapter.handle('getEntry', [ENTRY_ID]) as {headword: string};
    expect(entry.headword).toBe('nyumba');
  });
});

describe('edit permission', () => {
  it('rejects every write method for plugins that did not declare edit', async () => {
    const api = makeApi();
    const adapter = makeAdapter({api, permissions: []});
    for (const [method, args] of [
      ['createEntry', [{lexemeForm: {seh: 'x'}}]],
      ['updateEntry', [makeEntry(), makeEntry()]],
      ['applyChanges', [[]]],
      ['saveFile', [new ArrayBuffer(1), {filename: 'a.webm', mimeType: 'audio/webm'}]],
    ] as const) {
      await expect(async () => await adapter.handle(method, [...args]), method).rejects.toMatchObject({code: 'permission-denied'});
    }
    expect(api.createEntry).not.toHaveBeenCalled();
    expect(api.updateEntry).not.toHaveBeenCalled();
  });

  it('read methods work without any permissions', async () => {
    const adapter = makeAdapter({permissions: []});
    await expect(adapter.handle('getEntry', [ENTRY_ID])).resolves.toBeTruthy();
  });
});

describe('capability gating', () => {
  it('rejects comment and history methods when the host lacks the feature', async () => {
    const adapter = makeAdapter({capabilities: {comments: false, history: false}});
    await expect(async () => await adapter.handle('getCommentThread', [ENTRY_ID])).rejects.toMatchObject({code: 'not-supported'});
    await expect(async () => await adapter.handle('getEntityHistory', [ENTRY_ID])).rejects.toMatchObject({code: 'not-supported'});
    await expect(async () => await adapter.handle('getActivity', [{}])).rejects.toMatchObject({code: 'not-supported'});
  });
});

describe('updateEntry compare-and-swap', () => {
  it('applies and summarizes the true diff when before matches the current entry', async () => {
    const api = makeApi();
    let confirmed: PluginWriteOperation | undefined;
    const adapter = makeAdapter({api, confirmWrite: op => ((confirmed = op), Promise.resolve(true))});

    const before = makeEntry();
    const after = makeEntry({lexemeForm: {seh: 'yumba'}});
    await adapter.handle('updateEntry', [before, after]);

    expect(api.updateEntry).toHaveBeenCalled();
    expect(confirmed?.summary.join('\n')).toContain('Word (seh): "nyumba" → "yumba"');
  });

  it('rejects with conflict when before does not match the current entry (fabricated or stale)', async () => {
    // DB actually holds "precious data"; the plugin claims before was "cat" to disguise the overwrite.
    const api = makeApi({getEntry: vi.fn(() => Promise.resolve(makeEntry({citationForm: {seh: 'precious data'}})))});
    const adapter = makeAdapter({api});

    const fabricatedBefore = makeEntry({citationForm: {seh: 'cat'}});
    const after = makeEntry({citationForm: {seh: 'dog'}});
    await expect(async () => await adapter.handle('updateEntry', [fabricatedBefore, after]))
      .rejects.toMatchObject({code: 'conflict'});
    expect(api.updateEntry).not.toHaveBeenCalled();
  });

  it('rejects with conflict when the entry was deleted', async () => {
    const api = makeApi({getEntry: vi.fn(() => Promise.resolve(null))});
    const adapter = makeAdapter({api});
    await expect(async () => await adapter.handle('updateEntry', [makeEntry(), makeEntry({citationForm: {seh: 'x'}})]))
      .rejects.toMatchObject({code: 'conflict'});
  });

  it('skips the dialog and the write when nothing changed', async () => {
    const api = makeApi();
    const confirmWrite = vi.fn(() => Promise.resolve(true));
    const adapter = makeAdapter({api, confirmWrite});
    await adapter.handle('updateEntry', [makeEntry(), makeEntry()]);
    expect(confirmWrite).not.toHaveBeenCalled();
    expect(api.updateEntry).not.toHaveBeenCalled();
  });
});

describe('createEntry summary completeness', () => {
  it('surfaces every non-empty field, so nothing can be smuggled past the dialog', async () => {
    let confirmed: PluginWriteOperation | undefined;
    const adapter = makeAdapter({confirmWrite: op => ((confirmed = op), Promise.resolve(true))});

    await adapter.handle('createEntry', [{
      lexemeForm: {seh: 'nyumba'},
      note: {en: 'smuggled note'},
      literalMeaning: {en: 'house-place'},
      senses: [{gloss: {en: 'house'}, pictures: [{mediaUri: 'sil-media://x/1', caption: {}}]}],
    }]);

    const summary = confirmed!.summary.join('\n');
    expect(summary).toContain('Word (seh): "nyumba"');
    expect(summary).toContain('Note (en): "smuggled note"');
    expect(summary).toContain('Literal meaning (en): "house-place"');
    expect(summary).toContain('Sense 1 Gloss (en): "house"');
    expect(summary).toContain('Sense 1 Picture 1 File: "sil-media://x/1"');
  });

  it('declines reject with permission-denied and nothing is applied', async () => {
    const api = makeApi();
    const adapter = makeAdapter({api, confirmWrite: () => Promise.resolve(false)});
    await expect(async () => await adapter.handle('createEntry', [{lexemeForm: {seh: 'x'}}]))
      .rejects.toMatchObject({code: 'permission-denied'});
    await expect(async () => await adapter.handle('updateEntry', [makeEntry(), makeEntry({lexemeForm: {seh: 'b'}})]))
      .rejects.toMatchObject({code: 'permission-denied'});
    await expect(async () => await adapter.handle('applyChanges', [[{type: 'createEntry', entry: {lexemeForm: {seh: 'c'}}}]]))
      .rejects.toMatchObject({code: 'permission-denied'});
    expect(api.createEntry).not.toHaveBeenCalled();
    expect(api.updateEntry).not.toHaveBeenCalled();
  });
});

describe('saveFile budget', () => {
  const metadata = {filename: 'a.webm', mimeType: 'audio/webm'};

  it('stops accepting files after the per-run count', async () => {
    const saveFile = vi.fn(() => Promise.resolve({result: 'Success', mediaUri: 'sil-media://x/1'}));
    const adapter = makeAdapter({api: makeApi({saveFile})});
    for (let i = 0; i < MAX_SAVED_FILES_PER_RUN; i++) await adapter.handle('saveFile', [new ArrayBuffer(1), metadata]);
    await expect(async () => await adapter.handle('saveFile', [new ArrayBuffer(1), metadata]))
      .rejects.toMatchObject({code: 'storage-full'});
    expect(saveFile).toHaveBeenCalledTimes(MAX_SAVED_FILES_PER_RUN);
  });

  it('rejects a file that would exceed the per-run bytes without storing it', async () => {
    const saveFile = vi.fn();
    const adapter = makeAdapter({api: makeApi({saveFile})});
    const oversized = new Blob([new ArrayBuffer(1)]);
    Object.defineProperty(oversized, 'size', {value: MAX_SAVED_BYTES_PER_RUN + 1});
    await expect(async () => await adapter.handle('saveFile', [oversized, metadata]))
      .rejects.toMatchObject({code: 'storage-full'});
    expect(saveFile).not.toHaveBeenCalled();
  });
});

describe('summary sanitization', () => {
  it('strips bidi override and control characters that could disguise a change', async () => {
    let confirmed: PluginWriteOperation | undefined;
    const adapter = makeAdapter({confirmWrite: op => ((confirmed = op), Promise.resolve(true))});
    await adapter.handle('createEntry', [{lexemeForm: {seh: 'evil‮txt.exe'}}]);
    const summary = confirmed!.summary.join('\n');
    expect(summary).not.toContain('‮');
    expect(summary).toContain('txt.exe');
  });
});

describe('applyChanges', () => {
  it('shows ONE dialog covering the whole batch and applies in order', async () => {
    const api = makeApi();
    const confirmWrite = vi.fn((op: PluginWriteOperation) => {
      expect(op.kind).toBe('batch');
      expect(op.kind === 'batch' && op.count).toBe(2);
      return Promise.resolve(true);
    });
    const adapter = makeAdapter({api, confirmWrite});

    const results = await adapter.handle('applyChanges', [[
      {type: 'createEntry', entry: {lexemeForm: {seh: 'a'}}},
      {type: 'updateEntry', before: makeEntry(), after: makeEntry({lexemeForm: {seh: 'b'}})},
    ]]) as unknown[];

    expect(confirmWrite).toHaveBeenCalledTimes(1);
    expect(results).toHaveLength(2);
    expect(api.createEntry).toHaveBeenCalledTimes(1);
    expect(api.updateEntry).toHaveBeenCalledTimes(1);
  });

  it('a conflict in ANY operation fails the batch before anything is applied', async () => {
    const api = makeApi({getEntry: vi.fn(() => Promise.resolve(makeEntry({citationForm: {seh: 'someone edited this'}})))});
    const adapter = makeAdapter({api});
    await expect(async () => await adapter.handle('applyChanges', [[
      {type: 'createEntry', entry: {lexemeForm: {seh: 'a'}}},
      {type: 'updateEntry', before: makeEntry(), after: makeEntry({lexemeForm: {seh: 'b'}})},
    ]])).rejects.toMatchObject({code: 'conflict'});
    expect(api.createEntry).not.toHaveBeenCalled();
    expect(api.updateEntry).not.toHaveBeenCalled();
  });
});

describe('preview write mode', () => {
  function approve(_operation: PluginWriteOperation) {
    return Promise.resolve(true);
  }

  it('shows each write like a live run, then rejects with preview and applies nothing', async () => {
    const api = makeApi();
    const confirmWrite = vi.fn(approve);
    const adapter = makeAdapter({api, confirmWrite, writeMode: 'preview'});

    await expect(async () => await adapter.handle('createEntry', [{lexemeForm: {seh: 'a'}}]))
      .rejects.toMatchObject({code: 'preview'});
    await expect(async () => await adapter.handle('updateEntry', [makeEntry(), makeEntry({lexemeForm: {seh: 'b'}})]))
      .rejects.toMatchObject({code: 'preview'});
    await expect(async () => await adapter.handle('applyChanges', [[{type: 'createEntry', entry: {lexemeForm: {seh: 'c'}}}]]))
      .rejects.toMatchObject({code: 'preview'});

    expect(confirmWrite).toHaveBeenCalledTimes(3);
    expect(confirmWrite.mock.calls.map(([op]) => op.kind)).toEqual(['createEntry', 'updateEntry', 'batch']);
    expect(api.createEntry).not.toHaveBeenCalled();
    expect(api.updateEntry).not.toHaveBeenCalled();
  });

  it('still enforces compare-and-swap, so the preview is honest about a conflict', async () => {
    const api = makeApi({getEntry: vi.fn(() => Promise.resolve(makeEntry({citationForm: {seh: 'someone edited this'}})))});
    const confirmWrite = vi.fn(approve);
    const adapter = makeAdapter({api, confirmWrite, writeMode: 'preview'});
    await expect(async () => await adapter.handle('updateEntry', [makeEntry(), makeEntry({lexemeForm: {seh: 'b'}})]))
      .rejects.toMatchObject({code: 'conflict'});
    expect(confirmWrite).not.toHaveBeenCalled();
  });

  it('still requires the edit permission', async () => {
    const adapter = makeAdapter({permissions: [], writeMode: 'preview'});
    await expect(async () => await adapter.handle('createEntry', [{lexemeForm: {seh: 'a'}}]))
      .rejects.toMatchObject({code: 'permission-denied'});
  });

  it('rejects saveFile without storing anything', async () => {
    const saveFile = vi.fn();
    const adapter = makeAdapter({api: makeApi({saveFile}), writeMode: 'preview'});
    await expect(async () => await adapter.handle('saveFile', [new ArrayBuffer(1), {filename: 'a.webm', mimeType: 'audio/webm'}]))
      .rejects.toMatchObject({code: 'preview'});
    expect(saveFile).not.toHaveBeenCalled();
  });

  it('keeps plugin storage in memory, out of localStorage', async () => {
    const adapter = makeAdapter({
      writeMode: 'preview',
      storage: new PluginStorage('test-project', 'test-plugin', createMemoryStorageBackend()),
    });
    await adapter.handle('storageSet', ['progress', 3]);
    await expect(adapter.handle('storageGet', ['progress'])).resolves.toBe(3);
    expect(localStorage.length).toBe(0);
  });
});

describe('PluginApiException plumbing', () => {
  it('is an Error with a code', () => {
    const error = new PluginApiException('conflict', 'boom');
    expect(error).toBeInstanceOf(Error);
    expect(error.code).toBe('conflict');
  });
});
