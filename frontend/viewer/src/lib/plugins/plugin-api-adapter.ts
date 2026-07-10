import {ActivitySort, type IEntry, type IMiniLcmJsInvokable, type IWritingSystem, MorphTypeKind, SortField, SubjectType} from '$lib/dotnet-types';
import type {IQueryOptions} from '$lib/dotnet-types';
import type {IUploadFileResponse} from '$lib/dotnet-types/generated-types/MiniLcm/Media/IUploadFileResponse';
import type {IHistoryServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import {
  KNOWN_OPEN_ENTRY_MODES,
  type HostCapabilities,
  type OpenEntryMode,
  PluginApiException,
  type PluginEntryFilter,
  type PluginEntryQuery,
  type PluginHostFeature,
  type PluginPermission,
  type PluginWriteOperation,
} from './plugin-api-types';
import type {PluginStorage} from './plugin-local-data';
import {gt} from 'svelte-i18n-lingui';

export interface PluginHostCallbacks {
  /** Shows the write to the user; resolves true only if they approve it. */
  confirmWrite(operation: PluginWriteOperation): Promise<boolean>;
  openEntry(entryId: string, mode: OpenEntryMode): void;
  notify(message: string): void;
}

/** Entries handed to plugins carry a computed, read-only `headword` for convenience. */
export type PluginEntry = IEntry & {headword: string};

type MorphTokens = Partial<Record<MorphTypeKind, {prefix?: string; postfix?: string}>>;

const DEFAULT_ENTRY_LIMIT = 100;
const MAX_ENTRY_LIMIT = 1000;
/**
 * Backstop against pathological summaries, NOT a display budget: the confirm dialog scrolls, and
 * silently truncating a write summary would let junk lines push real changes out of sight.
 */
const MAX_SUMMARY_LINES = 1000;
const MAX_BATCH_OPERATIONS = 200;
const DEFAULT_ACTIVITY_TAKE = 50;
const MAX_ACTIVITY_TAKE = 500;

/**
 * The complete plugin-facing API, v1. One method here = one method plugins can call (the SDK's
 * `window.fwlite` surface, minus its pure-JS utilities). {@link PluginApiAdapter} implements it;
 * {@link argDecoders} validates the untyped postMessage args for it. Adding a method means adding
 * it in all three places — the compiler enforces that — plus plugin-sdk.js and plugin-prompt.ts.
 */
export interface PluginApiMethods {
  getWritingSystems(): Promise<unknown>;
  getEntries(query: PluginEntryQuery): Promise<PluginEntry[]>;
  countEntries(query: PluginEntryQuery): Promise<number>;
  getEntry(id: string): Promise<PluginEntry | null>;
  getPartsOfSpeech(): Promise<unknown>;
  getSemanticDomains(): Promise<unknown>;
  getMedia(mediaUri: string): Promise<{data: ArrayBuffer; fileName?: string; mimeType?: string} | null>;
  saveFile(data: unknown, metadata: unknown): Promise<IUploadFileResponse>;
  createEntry(entry: unknown): Promise<PluginEntry>;
  updateEntry(before: unknown, after: unknown): Promise<PluginEntry>;
  applyChanges(operations: unknown): Promise<PluginEntry[]>;
  openEntry(id: string, mode: OpenEntryMode): Promise<void>;
  notify(message: string): Promise<void>;
  // Comments (read-only; requires the `comments` host feature)
  getCommentThreads(options: unknown): Promise<unknown>;
  getCommentThread(threadId: string): Promise<unknown>;
  getUserComments(threadId: string): Promise<unknown>;
  getUnreadComments(threadId: string | undefined): Promise<unknown>;
  getUnreadCommentsForSubject(options: unknown): Promise<unknown>;
  countUnreadComments(threadId: string | undefined): Promise<unknown>;
  // Activity / history (read-only; requires the `history` host feature)
  getActivity(query: unknown): Promise<unknown>;
  getEntityHistory(entityId: string): Promise<unknown>;
  getChangeContext(options: unknown): Promise<unknown>;
  getObjectAtCommit(options: unknown): Promise<unknown>;
  listActivityAuthors(): Promise<unknown>;
  listActivityChangeTypes(): Promise<unknown>;
  // Per-plugin persistent storage
  storageGet(key: string): Promise<unknown>;
  storageSet(key: string, value: unknown): Promise<void>;
  storageRemove(key: string): Promise<void>;
}

type ArgDecoders = {
  [K in keyof PluginApiMethods]: (args: unknown[]) => Parameters<PluginApiMethods[K]>;
};

/**
 * Turns the untrusted `unknown[]` coming over postMessage into each method's typed arguments,
 * throwing `invalid-args` on anything malformed. The mapped type makes this table provably
 * complete: a method added to {@link PluginApiMethods} without a decoder fails to compile.
 */
const argDecoders: ArgDecoders = {
  getWritingSystems: () => [],
  getEntries: ([query]) => [asQuery(query)],
  countEntries: ([query]) => [asQuery(query)],
  getEntry: ([id]) => [asId(id)],
  getPartsOfSpeech: () => [],
  getSemanticDomains: () => [],
  getMedia: ([uri]) => [asNonEmptyString(uri, 'mediaUri')],
  saveFile: ([data, metadata]) => [data, metadata],
  createEntry: ([entry]) => [entry],
  updateEntry: ([before, after]) => [before, after],
  applyChanges: ([operations]) => [operations],
  openEntry: ([id, options]) => [asId(id), asOpenEntryMode(options)],
  notify: ([message]) => [asNonEmptyString(message, 'message')],
  getCommentThreads: ([options]) => [options],
  getCommentThread: ([id]) => [asId(id)],
  getUserComments: ([id]) => [asId(id)],
  getUnreadComments: ([options]) => [asOptionalId(readField(options, 'threadId'))],
  getUnreadCommentsForSubject: ([options]) => [options],
  countUnreadComments: ([options]) => [asOptionalId(readField(options, 'threadId'))],
  getActivity: ([query]) => [query],
  getEntityHistory: ([id]) => [asId(id)],
  getChangeContext: ([options]) => [options],
  getObjectAtCommit: ([options]) => [options],
  listActivityAuthors: () => [],
  listActivityChangeTypes: () => [],
  storageGet: ([key]) => [asNonEmptyString(key, 'key')],
  storageSet: ([key, value]) => [asNonEmptyString(key, 'key'), value],
  storageRemove: ([key]) => [asNonEmptyString(key, 'key')],
};

function isPluginApiMethod(method: string): method is keyof PluginApiMethods {
  return Object.hasOwn(argDecoders, method);
}

export interface PluginApiConfig {
  /** Permissions the running plugin declared (parsed from its actual HTML). */
  permissions: PluginPermission[];
  /** Optional features this host/project supports. */
  capabilities: Pick<HostCapabilities, 'comments' | 'history'>;
}

/**
 * Implements the plugin-facing API (v1) as a thin, deliberately small adapter over the MiniLcm
 * API. Every plugin request is dispatched through {@link handle}. Writes never reach MiniLcm
 * without user approval, and only for plugins that declared the `edit` permission.
 */
export class PluginApiAdapter implements PluginApiMethods {
  constructor(
    private api: IMiniLcmJsInvokable,
    private storage: PluginStorage,
    private config: PluginApiConfig,
    private callbacks: PluginHostCallbacks,
    private historyService?: IHistoryServiceJsInvokable,
  ) {}

  /** Loaded once per plugin session; writing systems and morph tokens don't change mid-run. */
  #headwordDeps?: Promise<{vernacular: IWritingSystem[]; morphTokens: MorphTokens}>;

  handle(method: string, args: unknown[]): Promise<unknown> {
    if (!isPluginApiMethod(method)) {
      throw new PluginApiException('unknown-method', `Unknown plugin API method: ${method}`);
    }
    const decodedArgs = argDecoders[method](args);
    return (this[method] as (...decoded: unknown[]) => Promise<unknown>)(...decodedArgs);
  }

  getWritingSystems(): Promise<unknown> {
    return this.api.getWritingSystems();
  }

  async getEntries(query: PluginEntryQuery): Promise<PluginEntry[]> {
    const options = toQueryOptions(query);
    const entries = query.search
      ? await this.api.searchEntries(query.search, options)
      : await this.api.getEntries(options);
    return await this.withHeadwords(entries);
  }

  async countEntries(query: PluginEntryQuery): Promise<number> {
    return await this.api.countEntries(query.search || undefined, {filter: toGridifyFilter(query.filter)});
  }

  async getEntry(id: string): Promise<PluginEntry | null> {
    const entry = await this.api.getEntry(id);
    if (!entry) return null;
    return (await this.withHeadwords([entry]))[0];
  }

  getPartsOfSpeech(): Promise<unknown> {
    return this.api.getPartsOfSpeech();
  }

  getSemanticDomains(): Promise<unknown> {
    return this.api.getSemanticDomains();
  }

  /**
   * Fetches the bytes for a media reference (an audio writing-system value, or a picture's
   * mediaUri). The app downloads the file automatically if needed; returns null when it can't be
   * had (offline, or not found), so plugins can degrade instead of crashing.
   */
  async getMedia(mediaUri: string): Promise<{data: ArrayBuffer; fileName?: string; mimeType?: string} | null> {
    const file = await this.api.getFileStream(mediaUri);
    if (!file.stream) return null;
    const data = await file.stream.arrayBuffer();
    return {data, fileName: file.fileName, mimeType: guessMimeType(file.fileName)};
  }

  /**
   * Stores a file (e.g. a recording or captured image) and returns its {@link IUploadFileResponse}
   * with a `mediaUri` you then write into an entry field (via updateEntry) to attach it. The bytes
   * themselves are stored locally; the entry edit that references them is still user-approved.
   */
  async saveFile(data: unknown, metadata: unknown): Promise<IUploadFileResponse> {
    this.#requireEditPermission();
    if (!(data instanceof ArrayBuffer) && !(data instanceof Uint8Array) && !(data instanceof Blob)) {
      throw new PluginApiException('invalid-args', 'saveFile requires the file bytes as an ArrayBuffer, Uint8Array, or Blob');
    }
    if (typeof metadata !== 'object' || metadata === null) {
      throw new PluginApiException('invalid-args', 'saveFile requires metadata {filename, mimeType}');
    }
    const filename = asNonEmptyString((metadata as {filename?: unknown}).filename, 'filename');
    const mimeType = asNonEmptyString((metadata as {mimeType?: unknown}).mimeType, 'mimeType');
    return await this.api.saveFile(data, {filename, mimeType});
  }

  async createEntry(input: unknown): Promise<PluginEntry> {
    this.#requireEditPermission();
    const prepared = this.prepareCreate(input);
    if (!await this.callbacks.confirmWrite(prepared.operation)) {
      throw new PluginApiException('permission-denied', 'The user declined this change');
    }
    return prepared.apply();
  }

  async updateEntry(beforeInput: unknown, afterInput: unknown): Promise<PluginEntry> {
    this.#requireEditPermission();
    const prepared = await this.prepareUpdate(beforeInput, afterInput);
    if (!prepared) return (await this.withHeadwords([stripHeadword(afterInput as IEntry)]))[0]; // nothing changed
    if (!await this.callbacks.confirmWrite(prepared.operation)) {
      throw new PluginApiException('permission-denied', 'The user declined this change');
    }
    return prepared.apply();
  }

  /** Applies several creates/updates behind a SINGLE approval dialog, then runs them in order. */
  async applyChanges(input: unknown): Promise<PluginEntry[]> {
    this.#requireEditPermission();
    if (!Array.isArray(input)) throw new PluginApiException('invalid-args', 'applyChanges requires an array of operations');
    if (input.length > MAX_BATCH_OPERATIONS) {
      throw new PluginApiException('invalid-args', `Too many operations in one batch (max ${MAX_BATCH_OPERATIONS})`);
    }
    // Prepare (validate + summarize) everything up front so a bad op fails before anything is applied.
    const prepared: PreparedWrite[] = [];
    for (const op of input) {
      const p = await this.prepareOperation(op);
      if (p) prepared.push(p);
    }
    if (prepared.length === 0) return [];
    const approved = await this.callbacks.confirmWrite({
      kind: 'batch',
      count: prepared.length,
      summary: buildBatchSummary(prepared.map(p => p.operation)),
    });
    if (!approved) throw new PluginApiException('permission-denied', 'The user declined these changes');
    const results: PluginEntry[] = [];
    for (const p of prepared) results.push(await p.apply());
    return results;
  }

  openEntry(id: string, mode: OpenEntryMode): Promise<void> {
    this.callbacks.openEntry(id, mode);
    return Promise.resolve();
  }

  notify(message: string): Promise<void> {
    this.callbacks.notify(message);
    return Promise.resolve();
  }

  getCommentThreads(input: unknown): Promise<unknown> {
    this.#requireFeature('comments');
    const {subjectType, subjectId} = input as {subjectType?: unknown; subjectId?: unknown};
    const includeComments = (input as {includeComments?: unknown})?.includeComments;
    return this.api.getCommentThreads(asSubjectType(subjectType), asId(subjectId), includeComments === true);
  }

  getCommentThread(threadId: string): Promise<unknown> {
    this.#requireFeature('comments');
    return this.api.getCommentThread(threadId);
  }

  getUserComments(threadId: string): Promise<unknown> {
    this.#requireFeature('comments');
    return this.api.getUserComments(threadId);
  }

  getUnreadComments(threadId: string | undefined): Promise<unknown> {
    this.#requireFeature('comments');
    return this.api.getUnreadComments(threadId);
  }

  getUnreadCommentsForSubject(input: unknown): Promise<unknown> {
    this.#requireFeature('comments');
    const {subjectType, subjectId} = input as {subjectType?: unknown; subjectId?: unknown};
    return this.api.getUnreadCommentsForSubject(asSubjectType(subjectType), asId(subjectId));
  }

  countUnreadComments(threadId: string | undefined): Promise<unknown> {
    this.#requireFeature('comments');
    return this.api.countUnreadComments(threadId);
  }

  getActivity(input: unknown): Promise<unknown> {
    const query = (input ?? {}) as {skip?: unknown; take?: unknown; authorFilterKeys?: unknown; changeTypeKeys?: unknown; sort?: unknown};
    const skip = Math.max(asOptionalNumber(query.skip) ?? 0, 0);
    const take = Math.min(Math.max(asOptionalNumber(query.take) ?? DEFAULT_ACTIVITY_TAKE, 1), MAX_ACTIVITY_TAKE);
    return this.history().projectActivity(skip, take, asOptionalStringArray(query.authorFilterKeys), asOptionalStringArray(query.changeTypeKeys), asActivitySort(query.sort));
  }

  getEntityHistory(entityId: string): Promise<unknown> {
    return this.history().getHistory(entityId);
  }

  getChangeContext(input: unknown): Promise<unknown> {
    const {commitId, changeIndex} = input as {commitId?: unknown; changeIndex?: unknown};
    return this.history().loadChangeContext(asId(commitId), asOptionalNumber(changeIndex) ?? 0);
  }

  getObjectAtCommit(input: unknown): Promise<unknown> {
    const {commitId, entityId} = input as {commitId?: unknown; entityId?: unknown};
    return this.history().getObject(asId(commitId), asId(entityId));
  }

  listActivityAuthors(): Promise<unknown> {
    return this.history().listActivityAuthors();
  }

  listActivityChangeTypes(): Promise<unknown> {
    return this.history().listActivityChangeTypes();
  }

  storageGet(key: string): Promise<unknown> {
    return Promise.resolve(this.storage.get(key));
  }

  storageSet(key: string, value: unknown): Promise<void> {
    this.storage.set(key, value);
    return Promise.resolve();
  }

  storageRemove(key: string): Promise<void> {
    this.storage.remove(key);
    return Promise.resolve();
  }

  #requireEditPermission(): void {
    if (this.config.permissions.includes('edit')) return;
    throw new PluginApiException('permission-denied',
      'This plugin has not declared the "edit" permission, so it cannot change the dictionary. ' +
      'Declare <meta name="fwlite-plugin-permissions" content="edit"> to request write access.');
  }

  #requireFeature(feature: PluginHostFeature): void {
    if (this.config.capabilities[feature]) return;
    throw new PluginApiException('not-supported', `This project does not support ${feature}`);
  }

  /** History lives on a separate service that isn't available for every project type. */
  private history(): IHistoryServiceJsInvokable {
    this.#requireFeature('history');
    if (!this.historyService) {
      throw new PluginApiException('not-supported', 'Activity/history is not available for this project');
    }
    return this.historyService;
  }

  /** Attaches the app's headword (citation form, else morph-decorated lexeme form) to each entry. */
  private async withHeadwords(entries: IEntry[]): Promise<PluginEntry[]> {
    const {vernacular, morphTokens} = await this.headwordDeps();
    return entries.map(entry => Object.assign(entry, {headword: computeHeadword(entry, vernacular, morphTokens)}));
  }

  private headwordDeps(): Promise<{vernacular: IWritingSystem[]; morphTokens: MorphTokens}> {
    return this.#headwordDeps ??= Promise.all([this.api.getWritingSystems(), this.api.getMorphTypes()])
      .then(([writingSystems, morphTypes]) => {
        const morphTokens: MorphTokens = {};
        for (const morphType of morphTypes) morphTokens[morphType.kind] = {prefix: morphType.prefix, postfix: morphType.postfix};
        return {vernacular: writingSystems.vernacular, morphTokens};
      });
  }

  private async prepareOperation(op: unknown): Promise<PreparedWrite | null> {
    if (typeof op !== 'object' || op === null) throw new PluginApiException('invalid-args', 'Each operation must be an object');
    const type = (op as {type?: unknown}).type;
    if (type === 'createEntry') return this.prepareCreate((op as {entry?: unknown}).entry);
    if (type === 'updateEntry') {
      const {before, after} = op as {before?: unknown; after?: unknown};
      return await this.prepareUpdate(before, after);
    }
    throw new PluginApiException('invalid-args', `Unknown operation type: ${String(type)}`);
  }

  private prepareCreate(input: unknown): PreparedWrite {
    if (typeof input !== 'object' || input === null) {
      throw new PluginApiException('invalid-args', 'createEntry requires an entry object');
    }
    const entry = normalizeNewEntry(stripHeadword(input as Partial<IEntry>));
    return {
      operation: {kind: 'createEntry', entry, summary: describeEntry(entry)},
      apply: async () => (await this.withHeadwords([
        await this.api.createEntry(entry, {includeComplexFormsAndComponents: false, autoAddMainPublication: true}),
      ]))[0],
    };
  }

  /**
   * updateEntry has compare-and-swap semantics: `before` must match the entry's CURRENT state or
   * the call fails with `conflict`. That's what makes the approval dialog trustworthy — the diff
   * it shows is guaranteed to be the real effect. Without the check, a plugin could fabricate
   * `before` so the dialog shows "cat → dog" while the write actually clobbers something else.
   */
  private async prepareUpdate(beforeInput: unknown, afterInput: unknown): Promise<PreparedWrite | null> {
    // Drop the computed `headword` so it never reaches the real diff/apply — it's not a model field.
    const before = stripHeadword(beforeInput as IEntry);
    const after = stripHeadword(afterInput as IEntry);
    if (!before?.id || !after?.id || before.id !== after.id) {
      throw new PluginApiException('invalid-args', 'updateEntry requires before and after versions of the same entry');
    }
    const current = await this.api.getEntry(before.id);
    if (!current) {
      throw new PluginApiException('conflict', 'The entry no longer exists — it may have been deleted');
    }
    if (diffSummary(before, current).length > 0) {
      throw new PluginApiException('conflict',
        'The entry changed since it was fetched — fetch it again, reapply your edit, and retry');
    }
    const summary = diffSummary(current, after);
    if (summary.length === 0) return null;
    return {
      operation: {kind: 'updateEntry', before, after, summary},
      apply: async () => (await this.withHeadwords([await this.api.updateEntry(before, after)]))[0],
    };
  }
}

interface PreparedWrite {
  operation: PluginWriteOperation;
  apply: () => Promise<PluginEntry>;
}

/** headword = citationForm (undecorated), else lexemeForm with the morph type's affix tokens. */
export function computeHeadword(entry: IEntry, vernacular: IWritingSystem[], morphTokens: MorphTokens): string {
  for (const ws of vernacular) {
    if (ws.isAudio) continue;
    const citation = entry.citationForm?.[ws.wsId];
    if (citation) return citation;
    const lexeme = entry.lexemeForm?.[ws.wsId];
    if (lexeme) {
      const token = morphTokens[entry.morphType];
      return `${token?.prefix ?? ''}${lexeme}${token?.postfix ?? ''}`;
    }
  }
  return '';
}

function stripHeadword<T>(entry: T): T {
  if (entry && typeof entry === 'object' && 'headword' in entry) {
    const {headword: _headword, ...rest} = entry as Record<string, unknown>;
    return rest as T;
  }
  return entry;
}

const MIME_BY_EXTENSION: Record<string, string> = {
  mp3: 'audio/mpeg', wav: 'audio/wav', ogg: 'audio/ogg', oga: 'audio/ogg', m4a: 'audio/mp4',
  aac: 'audio/aac', flac: 'audio/flac', webm: 'audio/webm', opus: 'audio/opus',
  jpg: 'image/jpeg', jpeg: 'image/jpeg', png: 'image/png', gif: 'image/gif', webp: 'image/webp',
  bmp: 'image/bmp', svg: 'image/svg+xml', tif: 'image/tiff', tiff: 'image/tiff',
};

/** Best-effort MIME from the file extension, so plugins can build a typed Blob for <audio>/<img>. */
function guessMimeType(fileName: string | undefined): string | undefined {
  const extension = fileName?.split('.').pop()?.toLowerCase();
  return extension ? MIME_BY_EXTENSION[extension] : undefined;
}

function asOpenEntryMode(value: unknown): OpenEntryMode {
  if (value === undefined || value === null) return 'view';
  if (typeof value !== 'object') throw new PluginApiException('invalid-args', 'openEntry options must be an object');
  const mode = (value as {mode?: unknown}).mode;
  if (mode === undefined) return 'view';
  if (typeof mode !== 'string' || !KNOWN_OPEN_ENTRY_MODES.includes(mode as OpenEntryMode)) {
    throw new PluginApiException('invalid-args', `Unknown openEntry mode: ${JSON.stringify(mode)}`);
  }
  return mode as OpenEntryMode;
}

function asQuery(value: unknown): PluginEntryQuery {
  if (value === undefined || value === null) return {};
  if (typeof value !== 'object') throw new PluginApiException('invalid-args', 'Query options must be an object');
  return value as PluginEntryQuery;
}

function asId(value: unknown): string {
  const id = asNonEmptyString(value, 'id');
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(id)) {
    throw new PluginApiException('invalid-args', `Not a valid id: ${id}`);
  }
  return id;
}

function asNonEmptyString(value: unknown, name: string): string {
  if (typeof value !== 'string' || !value) throw new PluginApiException('invalid-args', `${name} must be a non-empty string`);
  return value;
}

function asOptionalId(value: unknown): string | undefined {
  return value === undefined || value === null ? undefined : asId(value);
}

/** Reads a named field off an options object argument, tolerating a missing/omitted object. */
function readField(arg: unknown, name: string): unknown {
  if (arg === undefined || arg === null) return undefined;
  if (typeof arg !== 'object') throw new PluginApiException('invalid-args', 'Options must be an object');
  return (arg as Record<string, unknown>)[name];
}

function asOptionalNumber(value: unknown): number | undefined {
  if (value === undefined || value === null) return undefined;
  if (typeof value !== 'number' || !Number.isFinite(value)) throw new PluginApiException('invalid-args', 'Expected a number');
  return value;
}

function asOptionalStringArray(value: unknown): string[] | undefined {
  if (value === undefined || value === null) return undefined;
  if (!Array.isArray(value) || value.some(item => typeof item !== 'string')) {
    throw new PluginApiException('invalid-args', 'Expected an array of strings');
  }
  return value as string[];
}

function asSubjectType(value: unknown): SubjectType {
  if (typeof value !== 'string' || !Object.values(SubjectType).includes(value as SubjectType)) {
    throw new PluginApiException('invalid-args', `Not a valid subject type: ${String(value)}`);
  }
  return value as SubjectType;
}

function asActivitySort(value: unknown): ActivitySort {
  if (value === undefined || value === null) return ActivitySort.NewestFirst;
  if (typeof value !== 'string' || !Object.values(ActivitySort).includes(value as ActivitySort)) {
    throw new PluginApiException('invalid-args', `Not a valid activity sort: ${JSON.stringify(value)}`);
  }
  return value as ActivitySort;
}

function toQueryOptions(query: PluginEntryQuery): IQueryOptions {
  const limit = Math.min(Math.max(query.limit ?? DEFAULT_ENTRY_LIMIT, 1), MAX_ENTRY_LIMIT);
  const offset = Math.max(query.offset ?? 0, 0);
  const useRelevance = !!query.search && !query.sort;
  return {
    count: limit,
    offset,
    order: {
      field: useRelevance ? SortField.SearchRelevance : SortField.Headword,
      writingSystem: query.sort?.writingSystem ?? 'default',
      ascending: query.sort?.ascending ?? true,
    },
    filter: toGridifyFilter(query.filter),
  };
}

/**
 * Plugins only get structured filters; the gridify string syntax stays an internal detail.
 * Inputs are strictly validated since they end up inside a query expression.
 */
export function toGridifyFilter(filter: PluginEntryFilter | undefined): {gridifyFilter: string} | undefined {
  if (!filter) return undefined;
  const parts: string[] = [];
  if (filter.semanticDomainCode !== undefined) {
    if (!/^[0-9.]+$/.test(filter.semanticDomainCode)) {
      throw new PluginApiException('invalid-args', `Not a valid semantic domain code: ${filter.semanticDomainCode}`);
    }
    parts.push(`Senses.SemanticDomains.Code=${filter.semanticDomainCode}`);
  }
  if (filter.partOfSpeechId !== undefined) {
    parts.push(`Senses.PartOfSpeechId=${asId(filter.partOfSpeechId)}`);
  }
  // The missing-* expressions mirror the "provide-missing" task queries in tasks-service.ts.
  if (filter.missingGlossWs !== undefined) {
    parts.push(`(Senses=null|Senses.Gloss[${asWsId(filter.missingGlossWs)}]=)`);
  }
  if (filter.missingExampleWs !== undefined) {
    parts.push(`(Senses.ExampleSentences=null|Senses.ExampleSentences.Sentence[${asWsId(filter.missingExampleWs)}]=)`);
  }
  if (filter.missingPartOfSpeech) {
    parts.push('Senses.PartOfSpeechId=');
  }
  if (parts.length === 0) return undefined;
  return {gridifyFilter: parts.join(',')};
}

function asWsId(value: string): string {
  if (!/^[a-zA-Z][a-zA-Z0-9-]*$/.test(value)) {
    throw new PluginApiException('invalid-args', `Not a valid writing system code: ${value}`);
  }
  return value;
}

/** Fills in ids and required collections so a plugin can supply just the interesting fields. */
function normalizeNewEntry(input: Partial<IEntry>): IEntry {
  const entry: IEntry = {
    morphType: MorphTypeKind.Stem,
    homographNumber: 0,
    ...input,
    id: input.id || crypto.randomUUID(),
    lexemeForm: input.lexemeForm ?? {},
    citationForm: input.citationForm ?? {},
    literalMeaning: input.literalMeaning ?? {},
    note: input.note ?? {},
    senses: input.senses ?? [],
    components: [],
    complexForms: [],
    complexFormTypes: input.complexFormTypes ?? [],
    publishIn: input.publishIn ?? [],
  };
  for (const sense of entry.senses) {
    sense.id = sense.id || crypto.randomUUID();
    sense.entryId = entry.id;
    sense.gloss = sense.gloss ?? {};
    sense.definition = sense.definition ?? {};
    sense.semanticDomains = sense.semanticDomains ?? [];
    sense.exampleSentences = sense.exampleSentences ?? [];
    sense.pictures = sense.pictures ?? [];
    for (const example of sense.exampleSentences) {
      example.id = example.id || crypto.randomUUID();
      example.senseId = sense.id;
      example.sentence = example.sentence ?? {};
      example.translations = example.translations ?? [];
    }
  }
  return entry;
}

/** Keys that are structural noise in a write summary: generated ids and ordering bookkeeping. */
const SUMMARY_NOISE_KEYS = new Set(['id', 'entryId', 'senseId', 'order']);
/** App defaults normalizeNewEntry stamps on every create; a matching value says nothing. */
const SUMMARY_DEFAULT_VALUES: Record<string, unknown> = {morphType: MorphTypeKind.Stem, homographNumber: 0};

/**
 * Copy of a value with empty leaves ('', null, undefined, {}, []) and noise keys removed, so a
 * created entry's summary lists exactly the substance that will be stored. Returns undefined when
 * nothing remains.
 */
export function pruneForSummary(value: unknown): unknown {
  if (value === null || value === undefined || value === '') return undefined;
  if (Array.isArray(value)) {
    const pruned = value.map(pruneForSummary).filter(item => item !== undefined);
    return pruned.length === 0 ? undefined : pruned;
  }
  if (typeof value === 'object') {
    const result: Record<string, unknown> = {};
    for (const [key, item] of Object.entries(value)) {
      if (SUMMARY_NOISE_KEYS.has(key)) continue;
      if (key in SUMMARY_DEFAULT_VALUES && SUMMARY_DEFAULT_VALUES[key] === item) continue;
      const pruned = pruneForSummary(item);
      if (pruned !== undefined) result[key] = pruned;
    }
    return Object.keys(result).length === 0 ? undefined : result;
  }
  return value;
}

/**
 * One line per leaf value of the (pruned) entry, so the approval dialog shows EVERYTHING that will
 * be stored — a hand-picked field list here would let a plugin smuggle data through the fields the
 * list forgot.
 */
export function describeEntry(entry: IEntry): string[] {
  const lines: string[] = [];
  leafLines(pruneForSummary(entry), [], lines);
  return capLines(lines);
}

function leafLines(value: unknown, path: PathSegment[], lines: string[]): void {
  if (value === undefined) return;
  if (Array.isArray(value)) {
    value.forEach((item, index) => leafLines(item, [...path, index], lines));
    return;
  }
  if (typeof value === 'object' && value !== null) {
    for (const [key, item] of Object.entries(value)) {
      leafLines(item, [...path, key], lines);
    }
    return;
  }
  lines.push(`${formatPath(path)}: ${renderValue(value)}`);
}

/** Human-readable field-level diff between two JSON-ish values, for the write-approval dialog. */
export function diffSummary(before: unknown, after: unknown): string[] {
  const lines: string[] = [];
  diffInto(before, after, [], lines);
  return capLines(lines);
}

function diffInto(before: unknown, after: unknown, path: PathSegment[], lines: string[]): void {
  if (deepEqual(before, after)) return;
  const bothObjects =
    typeof before === 'object' && before !== null && !Array.isArray(before) &&
    typeof after === 'object' && after !== null && !Array.isArray(after);
  if (bothObjects) {
    const keys = new Set([...Object.keys(before), ...Object.keys(after)]);
    for (const key of keys) {
      diffInto(
        (before as Record<string, unknown>)[key],
        (after as Record<string, unknown>)[key],
        [...path, key],
        lines);
    }
    return;
  }
  if (Array.isArray(before) && Array.isArray(after)) {
    const length = Math.max(before.length, after.length);
    for (let i = 0; i < length; i++) {
      diffInto(before[i], after[i], [...path, i], lines);
    }
    return;
  }
  lines.push(`${formatPath(path)}: ${renderValue(before)} → ${renderValue(after)}`);
}

type PathSegment = string | number;

/**
 * The dialog's readers are dictionary editors, not developers, so paths render as the app's own
 * field names ("Sense 1 Gloss (en)"), falling back to the raw key for anything unknown — coverage
 * must never depend on this list being complete.
 */
function fieldLabel(key: string): string | undefined {
  switch (key) {
    case 'lexemeForm': return gt`Word`;
    case 'citationForm': return gt`Citation form`;
    case 'literalMeaning': return gt`Literal meaning`;
    case 'note': return gt`Note`;
    case 'morphType': return gt`Morph type`;
    case 'homographNumber': return gt`Homograph number`;
    case 'publishIn': return gt`Publish in`;
    case 'complexFormTypes': return gt`Complex form type`;
    case 'components': return gt`Component`;
    case 'complexForms': return gt`Complex form`;
    case 'senses': return gt`Sense`;
    case 'gloss': return gt`Gloss`;
    case 'definition': return gt`Definition`;
    case 'partOfSpeech': return gt`Part of speech`;
    case 'partOfSpeechId': return gt`Part of speech`;
    case 'semanticDomains': return gt`Semantic domain`;
    case 'exampleSentences': return gt`Example`;
    case 'sentence': return gt`Sentence`;
    case 'translations': return gt`Translation`;
    case 'reference': return gt`Reference`;
    case 'pictures': return gt`Picture`;
    case 'caption': return gt`Caption`;
    case 'mediaUri': return gt`File`;
    default: return undefined;
  }
}

function formatPath(segments: PathSegment[]): string {
  const parts: string[] = [];
  for (let i = 0; i < segments.length; i++) {
    const segment = segments[i];
    if (typeof segment === 'number') {
      // An index humanizes onto the collection's label: senses,0 → "Sense 1".
      if (parts.length > 0) parts[parts.length - 1] += ` ${segment + 1}`;
      else parts.push(`${segment + 1}`);
      continue;
    }
    const label = fieldLabel(segment);
    if (label !== undefined) parts.push(label);
    // An unknown trailing key is usually a writing system id: lexemeForm,seh → "Word (seh)".
    else if (i === segments.length - 1) parts.push(`(${segment})`);
    else parts.push(segment);
  }
  return parts.join(' ');
}

function deepEqual(a: unknown, b: unknown): boolean {
  if (a === b) return true;
  if (typeof a !== 'object' || typeof b !== 'object' || a === null || b === null) return false;
  if (Array.isArray(a) !== Array.isArray(b)) return false;
  if (Array.isArray(a) && Array.isArray(b)) {
    return a.length === b.length && a.every((item, index) => deepEqual(item, b[index]));
  }
  const aRecord = a as Record<string, unknown>;
  const bRecord = b as Record<string, unknown>;
  const keys = new Set([...Object.keys(aRecord), ...Object.keys(bRecord)]);
  for (const key of keys) {
    if (!deepEqual(aRecord[key], bRecord[key])) return false;
  }
  return true;
}

function renderValue(value: unknown): string {
  if (value === undefined || value === null || value === '') return gt`(empty)`;
  if (typeof value === 'string') return `"${shorten(value)}"`;
  if (typeof value === 'object' && !Array.isArray(value)) {
    // Referenced items (semantic domains, parts of speech, publications) read by their name.
    const named = value as {code?: unknown; name?: unknown};
    if (typeof named.name === 'object' && named.name !== null) {
      const name = Object.values(named.name as Record<string, unknown>).find(candidate => typeof candidate === 'string' && candidate);
      if (typeof name === 'string') {
        return typeof named.code === 'string' && named.code ? `${named.code} ${shorten(name)}` : `"${shorten(name)}"`;
      }
    }
  }
  return shorten(JSON.stringify(value));
}

function shorten(value: string): string {
  return value.length > 80 ? value.slice(0, 77) + '…' : value;
}

// Bidi embedding/override/isolate marks and control characters can visually reorder or hide parts
// of a summary line, making the dialog read differently from what is applied — strip them.
// eslint-disable-next-line no-control-regex
const SUMMARY_UNSAFE_CHARS = /[\u0000-\u001F\u007F\u200E\u200F\u202A-\u202E\u2066-\u2069]/g;

function sanitizeLine(line: string): string {
  return line.replace(SUMMARY_UNSAFE_CHARS, ' ');
}

function capLines(lines: string[]): string[] {
  const sanitized = lines.map(sanitizeLine);
  if (sanitized.length <= MAX_SUMMARY_LINES) return sanitized;
  const hidden = sanitized.length - MAX_SUMMARY_LINES;
  return [...sanitized.slice(0, MAX_SUMMARY_LINES), gt`…and ${hidden} more changes`];
}

/** Flattens each batched op's summary under a numbered header, for the single confirmation dialog. */
function buildBatchSummary(operations: PluginWriteOperation[]): string[] {
  const lines: string[] = [];
  operations.forEach((operation, index) => {
    const label = operation.kind === 'createEntry' ? gt`Add entry` : gt`Change entry`;
    lines.push(`${index + 1}. ${label}:`);
    for (const line of operation.summary) lines.push(`   ${line}`);
  });
  return capLines(lines);
}
