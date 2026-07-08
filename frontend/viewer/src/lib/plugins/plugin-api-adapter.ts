import {ActivitySort, type IEntry, type IMiniLcmJsInvokable, type IWritingSystem, MorphTypeKind, SortField, SubjectType} from '$lib/dotnet-types';
import type {IQueryOptions} from '$lib/dotnet-types';
import type {IUploadFileResponse} from '$lib/dotnet-types/generated-types/MiniLcm/Media/IUploadFileResponse';
import type {IHistoryServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import {
  KNOWN_OPEN_ENTRY_MODES,
  type OpenEntryMode,
  PluginApiException,
  type PluginEntryFilter,
  type PluginEntryQuery,
  type PluginWriteOperation,
} from './plugin-api-types';
import type {PluginStorage} from './plugin-local-data';

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
const MAX_SUMMARY_LINES = 25;
const MAX_BATCH_OPERATIONS = 200;
const DEFAULT_ACTIVITY_TAKE = 50;
const MAX_ACTIVITY_TAKE = 500;

/**
 * Implements the plugin-facing API (v1) as a thin, deliberately small adapter over the MiniLcm
 * API. Every method a plugin can call is dispatched through {@link handle}; anything else gets
 * an unknown-method error. Writes never reach MiniLcm without user approval.
 */
export class PluginApiAdapter {
  constructor(
    private api: IMiniLcmJsInvokable,
    private storage: PluginStorage,
    private callbacks: PluginHostCallbacks,
    private historyService?: IHistoryServiceJsInvokable,
  ) {}

  /** Loaded once per plugin session; writing systems and morph tokens don't change mid-run. */
  #headwordDeps?: Promise<{vernacular: IWritingSystem[]; morphTokens: MorphTokens}>;

  handle(method: string, args: unknown[]): Promise<unknown> {
    switch (method) {
      case 'getWritingSystems': return this.api.getWritingSystems();
      case 'getEntries': return this.getEntries(asQuery(args[0]));
      case 'countEntries': return this.countEntries(asQuery(args[0]));
      case 'getEntry': return this.getEntry(asId(args[0]));
      case 'getPartsOfSpeech': return this.api.getPartsOfSpeech();
      case 'getSemanticDomains': return this.api.getSemanticDomains();
      case 'getMedia': return this.getMedia(asNonEmptyString(args[0], 'mediaUri'));
      case 'saveFile': return this.saveFile(args[0], args[1]);
      case 'createEntry': return this.createEntry(args[0]);
      case 'updateEntry': return this.updateEntry(args[0], args[1]);
      case 'applyChanges': return this.applyChanges(args[0]);
      case 'openEntry': return Promise.resolve(this.callbacks.openEntry(asId(args[0]), asOpenEntryMode(args[1])));
      case 'notify': return Promise.resolve(this.callbacks.notify(asNonEmptyString(args[0], 'message')));
      // Comments (read-only)
      case 'getCommentThreads': return this.getCommentThreads(args[0]);
      case 'getCommentThread': return this.api.getCommentThread(asId(args[0]));
      case 'getUserComments': return this.api.getUserComments(asId(args[0]));
      case 'getUnreadComments': return this.api.getUnreadComments(asOptionalId(readField(args[0], 'threadId')));
      case 'getUnreadCommentsForSubject': return this.getUnreadCommentsForSubject(args[0]);
      case 'countUnreadComments': return this.api.countUnreadComments(asOptionalId(readField(args[0], 'threadId')));
      // Activity / history (read-only)
      case 'getActivity': return this.getActivity(args[0]);
      case 'getEntityHistory': return this.history().getHistory(asId(args[0]));
      case 'getChangeContext': return this.getChangeContext(args[0]);
      case 'getObjectAtCommit': return this.getObjectAtCommit(args[0]);
      case 'listActivityAuthors': return this.history().listActivityAuthors();
      case 'listActivityChangeTypes': return this.history().listActivityChangeTypes();
      case 'storageGet': return Promise.resolve(this.storage.get(asNonEmptyString(args[0], 'key')));
      case 'storageSet': return Promise.resolve(this.storage.set(asNonEmptyString(args[0], 'key'), args[1]));
      case 'storageRemove': return Promise.resolve(this.storage.remove(asNonEmptyString(args[0], 'key')));
      default: throw new PluginApiException('unknown-method', `Unknown plugin API method: ${method}`);
    }
  }

  private async getEntries(query: PluginEntryQuery): Promise<PluginEntry[]> {
    const options = toQueryOptions(query);
    const entries = query.search
      ? await this.api.searchEntries(query.search, options)
      : await this.api.getEntries(options);
    return await this.withHeadwords(entries);
  }

  private async getEntry(id: string): Promise<PluginEntry | null> {
    const entry = await this.api.getEntry(id);
    if (!entry) return null;
    return (await this.withHeadwords([entry]))[0];
  }

  private async countEntries(query: PluginEntryQuery): Promise<number> {
    return await this.api.countEntries(query.search || undefined, {filter: toGridifyFilter(query.filter)});
  }

  /**
   * Fetches the bytes for a media reference (an audio writing-system value, or a picture's
   * mediaUri). The app downloads the file automatically if needed; returns null when it can't be
   * had (offline, or not found), so plugins can degrade instead of crashing.
   */
  private async getMedia(mediaUri: string): Promise<{data: ArrayBuffer; fileName?: string; mimeType?: string} | null> {
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
  private async saveFile(data: unknown, metadata: unknown): Promise<IUploadFileResponse> {
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

  private getCommentThreads(input: unknown) {
    const {subjectType, subjectId} = input as {subjectType?: unknown; subjectId?: unknown};
    const includeComments = (input as {includeComments?: unknown})?.includeComments;
    return this.api.getCommentThreads(asSubjectType(subjectType), asId(subjectId), includeComments === true);
  }

  private getUnreadCommentsForSubject(input: unknown) {
    const {subjectType, subjectId} = input as {subjectType?: unknown; subjectId?: unknown};
    return this.api.getUnreadCommentsForSubject(asSubjectType(subjectType), asId(subjectId));
  }

  private getActivity(input: unknown) {
    const query = (input ?? {}) as {skip?: unknown; take?: unknown; authorFilterKeys?: unknown; changeTypeKeys?: unknown; sort?: unknown};
    const skip = Math.max(asOptionalNumber(query.skip) ?? 0, 0);
    const take = Math.min(Math.max(asOptionalNumber(query.take) ?? DEFAULT_ACTIVITY_TAKE, 1), MAX_ACTIVITY_TAKE);
    return this.history().projectActivity(skip, take, asOptionalStringArray(query.authorFilterKeys), asOptionalStringArray(query.changeTypeKeys), asActivitySort(query.sort));
  }

  private getChangeContext(input: unknown) {
    const {commitId, changeIndex} = input as {commitId?: unknown; changeIndex?: unknown};
    return this.history().loadChangeContext(asId(commitId), asOptionalNumber(changeIndex) ?? 0);
  }

  private getObjectAtCommit(input: unknown) {
    const {commitId, entityId} = input as {commitId?: unknown; entityId?: unknown};
    return this.history().getObject(asId(commitId), asId(entityId));
  }

  /** History lives on a separate service that isn't available for every project type. */
  private history(): IHistoryServiceJsInvokable {
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

  private async createEntry(input: unknown): Promise<PluginEntry> {
    const prepared = this.prepareCreate(input);
    if (!await this.callbacks.confirmWrite(prepared.operation)) {
      throw new PluginApiException('permission-denied', 'The user declined this change');
    }
    return prepared.apply();
  }

  private async updateEntry(beforeInput: unknown, afterInput: unknown): Promise<PluginEntry> {
    const prepared = this.prepareUpdate(beforeInput, afterInput);
    if (!prepared) return (await this.withHeadwords([stripHeadword(afterInput as IEntry)]))[0]; // nothing changed
    if (!await this.callbacks.confirmWrite(prepared.operation)) {
      throw new PluginApiException('permission-denied', 'The user declined this change');
    }
    return prepared.apply();
  }

  /** Applies several creates/updates behind a SINGLE approval dialog, then runs them in order. */
  private async applyChanges(input: unknown): Promise<PluginEntry[]> {
    if (!Array.isArray(input)) throw new PluginApiException('invalid-args', 'applyChanges requires an array of operations');
    if (input.length > MAX_BATCH_OPERATIONS) {
      throw new PluginApiException('invalid-args', `Too many operations in one batch (max ${MAX_BATCH_OPERATIONS})`);
    }
    // Prepare (validate + summarize) everything up front so a bad op fails before anything is applied.
    const prepared = input.flatMap(op => {
      const p = this.prepareOperation(op);
      return p ? [p] : [];
    });
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

  private prepareOperation(op: unknown): {operation: PluginWriteOperation; apply: () => Promise<PluginEntry>} | null {
    if (typeof op !== 'object' || op === null) throw new PluginApiException('invalid-args', 'Each operation must be an object');
    const type = (op as {type?: unknown}).type;
    if (type === 'createEntry') return this.prepareCreate((op as {entry?: unknown}).entry);
    if (type === 'updateEntry') {
      const {before, after} = op as {before?: unknown; after?: unknown};
      return this.prepareUpdate(before, after);
    }
    throw new PluginApiException('invalid-args', `Unknown operation type: ${String(type)}`);
  }

  private prepareCreate(input: unknown): {operation: PluginWriteOperation; apply: () => Promise<PluginEntry>} {
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

  private prepareUpdate(beforeInput: unknown, afterInput: unknown): {operation: PluginWriteOperation; apply: () => Promise<PluginEntry>} | null {
    // Drop the computed `headword` so it never reaches the real diff/apply — it's not a model field.
    const before = stripHeadword(beforeInput as IEntry);
    const after = stripHeadword(afterInput as IEntry);
    if (!before?.id || !after?.id || before.id !== after.id) {
      throw new PluginApiException('invalid-args', 'updateEntry requires before and after versions of the same entry');
    }
    const summary = diffSummary(before, after);
    if (summary.length === 0) return null;
    return {
      operation: {kind: 'updateEntry', before, after, summary},
      apply: async () => (await this.withHeadwords([await this.api.updateEntry(before, after)]))[0],
    };
  }
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
    throw new PluginApiException('invalid-args', `Unknown openEntry mode: ${String(mode)}`);
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
    throw new PluginApiException('invalid-args', `Not a valid activity sort: ${String(value)}`);
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

function describeEntry(entry: IEntry): string[] {
  const lines: string[] = [];
  for (const [ws, value] of Object.entries(entry.lexemeForm)) {
    lines.push(`Word (${ws}): ${shorten(value)}`);
  }
  for (const [ws, value] of Object.entries(entry.citationForm)) {
    lines.push(`Citation form (${ws}): ${shorten(value)}`);
  }
  entry.senses.forEach((sense, index) => {
    for (const [ws, value] of Object.entries(sense.gloss)) {
      lines.push(`Sense ${index + 1} gloss (${ws}): ${shorten(value)}`);
    }
    for (const [ws, value] of Object.entries(sense.definition)) {
      lines.push(`Sense ${index + 1} definition (${ws}): ${shorten(richToText(value))}`);
    }
    for (const domain of sense.semanticDomains) {
      lines.push(`Sense ${index + 1} semantic domain: ${domain.code} ${shorten(Object.values(domain.name)[0] ?? '')}`);
    }
    if (sense.partOfSpeech) {
      lines.push(`Sense ${index + 1} part of speech: ${shorten(Object.values(sense.partOfSpeech.name)[0] ?? '')}`);
    }
  });
  return capLines(lines);
}

/** Human-readable field-level diff between two JSON-ish values, for the write-approval dialog. */
export function diffSummary(before: unknown, after: unknown): string[] {
  const lines: string[] = [];
  diffInto(before, after, '', lines);
  return capLines(lines);
}

function diffInto(before: unknown, after: unknown, path: string, lines: string[]): void {
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
        path ? `${path}.${key}` : key,
        lines);
    }
    return;
  }
  if (Array.isArray(before) && Array.isArray(after)) {
    const length = Math.max(before.length, after.length);
    for (let i = 0; i < length; i++) {
      diffInto(before[i], after[i], `${path}[${i}]`, lines);
    }
    return;
  }
  lines.push(`${path}: ${renderValue(before)} → ${renderValue(after)}`);
}

function deepEqual(a: unknown, b: unknown): boolean {
  if (a === b) return true;
  if (typeof a !== 'object' || typeof b !== 'object' || a === null || b === null) return false;
  return JSON.stringify(a) === JSON.stringify(b);
}

function renderValue(value: unknown): string {
  if (value === undefined || value === null || value === '') return '(empty)';
  if (typeof value === 'string') return `"${shorten(value)}"`;
  return shorten(JSON.stringify(value));
}

function richToText(value: unknown): string {
  if (typeof value === 'string') return value;
  if (typeof value === 'object' && value !== null && Array.isArray((value as {spans?: unknown}).spans)) {
    return ((value as {spans: {text?: string}[]}).spans).map(span => span.text ?? '').join('');
  }
  return JSON.stringify(value);
}

function shorten(value: string): string {
  return value.length > 80 ? value.slice(0, 77) + '…' : value;
}

function capLines(lines: string[]): string[] {
  if (lines.length <= MAX_SUMMARY_LINES) return lines;
  return [...lines.slice(0, MAX_SUMMARY_LINES), `…and ${lines.length - MAX_SUMMARY_LINES} more changes`];
}

/** Flattens each batched op's summary under a numbered header, for the single confirmation dialog. */
function buildBatchSummary(operations: PluginWriteOperation[]): string[] {
  const lines: string[] = [];
  operations.forEach((operation, index) => {
    const label = operation.kind === 'createEntry' ? 'Add entry' : 'Change entry';
    lines.push(`${index + 1}. ${label}:`);
    for (const line of operation.summary) lines.push(`   ${line}`);
  });
  return capLines(lines);
}
