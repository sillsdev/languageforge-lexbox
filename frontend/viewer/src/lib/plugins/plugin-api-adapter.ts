import {type IEntry, type IMiniLcmJsInvokable, MorphTypeKind, SortField} from '$lib/dotnet-types';
import type {IQueryOptions} from '$lib/dotnet-types';
import {
  PluginApiException,
  type PluginEntryFilter,
  type PluginEntryQuery,
  type PluginWriteOperation,
} from './plugin-api-types';
import type {PluginStorage} from './plugin-local-data';

export interface PluginHostCallbacks {
  /** Shows the write to the user; resolves true only if they approve it. */
  confirmWrite(operation: PluginWriteOperation): Promise<boolean>;
  openEntry(entryId: string): void;
  notify(message: string): void;
}

const DEFAULT_ENTRY_LIMIT = 100;
const MAX_ENTRY_LIMIT = 1000;
const MAX_SUMMARY_LINES = 25;

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
  ) {}

  handle(method: string, args: unknown[]): Promise<unknown> {
    switch (method) {
      case 'getWritingSystems': return this.api.getWritingSystems();
      case 'getEntries': return this.getEntries(asQuery(args[0]));
      case 'countEntries': return this.countEntries(asQuery(args[0]));
      case 'getEntry': return this.api.getEntry(asId(args[0]));
      case 'getPartsOfSpeech': return this.api.getPartsOfSpeech();
      case 'getSemanticDomains': return this.api.getSemanticDomains();
      case 'createEntry': return this.createEntry(args[0]);
      case 'updateEntry': return this.updateEntry(args[0], args[1]);
      case 'openEntry': return Promise.resolve(this.callbacks.openEntry(asId(args[0])));
      case 'notify': return Promise.resolve(this.callbacks.notify(asNonEmptyString(args[0], 'message')));
      case 'storageGet': return Promise.resolve(this.storage.get(asNonEmptyString(args[0], 'key')));
      case 'storageSet': return Promise.resolve(this.storage.set(asNonEmptyString(args[0], 'key'), args[1]));
      case 'storageRemove': return Promise.resolve(this.storage.remove(asNonEmptyString(args[0], 'key')));
      default: throw new PluginApiException('unknown-method', `Unknown plugin API method: ${method}`);
    }
  }

  private async getEntries(query: PluginEntryQuery): Promise<IEntry[]> {
    const options = toQueryOptions(query);
    if (query.search) return await this.api.searchEntries(query.search, options);
    return await this.api.getEntries(options);
  }

  private async countEntries(query: PluginEntryQuery): Promise<number> {
    return await this.api.countEntries(query.search || undefined, {filter: toGridifyFilter(query.filter)});
  }

  private async createEntry(input: unknown): Promise<IEntry> {
    if (typeof input !== 'object' || input === null) {
      throw new PluginApiException('invalid-args', 'createEntry requires an entry object');
    }
    const entry = normalizeNewEntry(input as Partial<IEntry>);
    const approved = await this.callbacks.confirmWrite({
      kind: 'createEntry',
      entry,
      summary: describeEntry(entry),
    });
    if (!approved) throw new PluginApiException('permission-denied', 'The user declined this change');
    return await this.api.createEntry(entry, {includeComplexFormsAndComponents: false, autoAddMainPublication: true});
  }

  private async updateEntry(beforeInput: unknown, afterInput: unknown): Promise<IEntry> {
    const before = beforeInput as IEntry;
    const after = afterInput as IEntry;
    if (!before?.id || !after?.id || before.id !== after.id) {
      throw new PluginApiException('invalid-args', 'updateEntry requires before and after versions of the same entry');
    }
    const summary = diffSummary(before, after);
    if (summary.length === 0) return after;
    const approved = await this.callbacks.confirmWrite({kind: 'updateEntry', before, after, summary});
    if (!approved) throw new PluginApiException('permission-denied', 'The user declined this change');
    return await this.api.updateEntry(before, after);
  }
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
