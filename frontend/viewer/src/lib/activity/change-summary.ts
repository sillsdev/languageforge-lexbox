/* eslint-disable @typescript-eslint/naming-convention -- object keys here are backend change-type discriminators, entity-type suffixes, and wire-format property names; all are intentionally PascalCase */
import {entityFieldIds, type FieldId} from '$lib/views/entity-config';
import type {IChangeEntity} from '$lib/dotnet-types';
import type {IActivityChangeInfo} from '$lib/dotnet-types/generated-types/LcmCrdt/IActivityChangeInfo';

export type SummaryEntity = 'entry' | 'sense' | 'example';
export type CollectionKind = 'senses' | 'examples' | 'components' | 'writingSystems';
export type ObjectKind =
  | 'partOfSpeech'
  | 'semanticDomain'
  | 'publication'
  | 'complexFormType'
  | 'writingSystem'
  | 'morphType'
  | 'customView';
/** Plural noun for a bulk-create collapse ("Created 100 semantic domains"). Keys mirror {@link BULK_CREATE_NOUNS}. */
export type BulkNoun =
  | 'entries'
  | 'senses'
  | 'examples'
  | 'partsOfSpeech'
  | 'semanticDomains'
  | 'publications'
  | 'complexFormTypes'
  | 'morphTypes'
  | 'writingSystems'
  | 'customViews';

/**
 * A localization-free description of one thing that happened in a change. The render component
 * ({@link ChangeSummary.svelte}) turns these into localized sentences. Kept pure so it's unit-testable.
 * Labels are pulled from the change payload when free; an id-only change carries no label (we don't
 * resolve names per row — that would be a DB hit per activity).
 */
export type ChangeFact =
  | {kind: 'create'; entity: SummaryEntity; label?: string; audioOnly?: boolean}
  | {kind: 'delete'; entity: SummaryEntity}
  // `audio` marks a media value (a recording): the value/ws are dropped and the summary shows an audio marker.
  | {kind: 'setField'; entity: SummaryEntity; fieldId: FieldId; ws?: string; value: string; audio?: boolean}
  // The system-assigned homograph number (a jsonPatch on a non-view field) — surfaced specifically, not as a generic patch.
  | {kind: 'setHomograph'; value: string}
  | {kind: 'clearField'; entity: SummaryEntity; fieldId: FieldId; ws?: string; audio?: boolean}
  | {kind: 'changeField'; entity: SummaryEntity; fieldId: FieldId}
  | {kind: 'addItem'; entity: SummaryEntity; fieldId: FieldId; label?: string}
  | {kind: 'removeItem'; entity: SummaryEntity; fieldId: FieldId}
  | {kind: 'replaceItem'; entity: SummaryEntity; fieldId: FieldId; label?: string}
  | {kind: 'reorder'; collection: CollectionKind}
  | {kind: 'moveSense'}
  | {kind: 'componentLink'; action: 'add' | 'update' | 'remove'}
  | {kind: 'setDefaultTranslation'}
  // A sense picture (a develop feature): added/removed/updated one, or reordered the list. Subject is the sense.
  | {kind: 'sensePicture'; action: 'add' | 'remove' | 'update' | 'reorder'}
  // The publication marked as the project's main one. Subject is the publication.
  | {kind: 'setMainPublication'}
  | {kind: 'createObject'; object: ObjectKind; label?: string}
  | {kind: 'editObject'; object: ObjectKind; label?: string}
  // A decoded field edit on a vocab object. `field` is humanized from the patch path (vocab objects have no field-label config).
  | {kind: 'editObjectField'; object: ObjectKind; field: string; ws?: string; value?: string; cleared?: boolean}
  | {kind: 'deleteObject'; object: ObjectKind}
  // A whole commit's worth of same-type creations collapsed to a count ("Created 100 semantic domains").
  | {kind: 'bulkCreate'; noun: BulkNoun; count: number}
  // A media resource (a substrate file — currently only audio recordings). `audio` is set when the commit
  // references this resource id from an audio writing system; `resourceId` lets the preview play it.
  | {kind: 'mediaResource'; action: 'add' | 'delete' | 'upload'; resourceId?: string; audio?: boolean}
  | {kind: 'generic'; text: string};

const MAIN_ENTITY_BY_SUFFIX: Record<string, SummaryEntity> = {
  Entry: 'entry',
  Sense: 'sense',
  ExampleSentence: 'example',
};

const OBJECT_BY_SUFFIX: Record<string, ObjectKind> = {
  PartOfSpeech: 'partOfSpeech',
  SemanticDomain: 'semanticDomain',
  Publication: 'publication',
  ComplexFormType: 'complexFormType',
  WritingSystem: 'writingSystem',
  MorphType: 'morphType',
  CustomView: 'customView',
};

const ORDER_COLLECTION: Record<string, CollectionKind> = {
  Sense: 'senses',
  ExampleSentence: 'examples',
  ComplexFormComponent: 'components',
  WritingSystem: 'writingSystems',
};

/**
 * The serializer used for the change payload over JSInterop is ambiguous between camelCase and PascalCase,
 * so read every property case-insensitively. ($type is read directly — it has no case variants.)
 */
function prop(obj: unknown, key: string): unknown {
  if (!obj || typeof obj !== 'object') return undefined;
  const record = obj as Record<string, unknown>;
  const camel = key[0].toLowerCase() + key.slice(1);
  const pascal = key[0].toUpperCase() + key.slice(1);
  return record[camel] ?? record[pascal];
}

function changeType(change: unknown): string {
  if (!change || typeof change !== 'object') return '';
  return ((change as Record<string, unknown>)['$type'] as string) ?? '';
}

/** Plain text of a value that can be shown inline: a string, number, or rich string ({spans:[{text}]}). */
function displayValue(value: unknown): string | undefined {
  if (value === null || value === undefined) return undefined;
  if (typeof value === 'string') return value;
  if (typeof value === 'number' || typeof value === 'boolean') return String(value);
  const spans = prop(value, 'spans');
  if (Array.isArray(spans)) return spans.map((s) => displayValue(prop(s, 'text')) ?? '').join('');
  return undefined;
}

// An audio writing system stores a media URI (sil-media://…), never human-readable text. Mirrors the
// backend WritingSystemId.IsAudio rule: script subtag Zxxx + the x-audio private-use variant.
function isAudioWs(wsId: string): boolean {
  const id = wsId.toLowerCase();
  return id.includes('-zxxx-') && id.includes('x-audio');
}

/** A media reference stored as a field value (audio recordings, and any other sil-media resource). */
function isMediaUri(value: string): boolean {
  return value.startsWith('sil-media:');
}

/** First non-empty NON-audio alternative of a MultiString or RichMultiString (audio values are media URIs, not text). */
function firstAlternative(multiString: unknown): string | undefined {
  if (!multiString || typeof multiString !== 'object') return undefined;
  for (const [wsId, value] of Object.entries(multiString as Record<string, unknown>)) {
    if (isAudioWs(wsId)) continue;
    const text = displayValue(value);
    if (text) return text;
  }
  return undefined;
}

/** Whether a MultiString has any audio-WS content — so an audio-only field can be summarized as "with audio". */
function hasAudioContent(multiString: unknown): boolean {
  if (!multiString || typeof multiString !== 'object') return false;
  return Object.entries(multiString as Record<string, unknown>).some(([wsId, value]) => isAudioWs(wsId) && !!displayValue(value));
}

/** The resource's own id (its EntityId), which equals the FileId in the sil-media URIs that reference it. */
function resourceIdOf(change: unknown): string | undefined {
  const id = prop(change, 'entityId');
  return typeof id === 'string' ? id : undefined;
}

/** A label from a plain string (e.g. a semantic-domain Code) or a MultiString Name. */
function labelOf(value: unknown): string | undefined {
  if (typeof value === 'string') return value || undefined;
  return firstAlternative(value);
}

/** A semantic domain's display label — code plus the first name alternative ("5.2 Food"), matching how the app shows domains. */
function semanticDomainLabel(semanticDomain: unknown): string | undefined {
  const code = labelOf(prop(semanticDomain, 'code'));
  const name = firstAlternative(prop(semanticDomain, 'name'));
  return code && name ? `${code} ${name}` : code ?? name;
}

function normalizeFieldId(entity: SummaryEntity, pathSegment: string): FieldId | undefined {
  const target = pathSegment.toLowerCase();
  // entityFieldIds typed against the SummaryEntity union widens to never[], so compare as plain strings.
  const ids = entityFieldIds(entity) as string[];
  return ids.find((id) => id.toLowerCase() === target) as FieldId | undefined;
}

// Past-tense forms for the leading verb of a change type, so the generic fallback matches the past tense
// of every purpose-built summary ("Created …", not "Create …"). Unknown leading words are just capitalized.
const PAST_TENSE_VERB: Record<string, string> = {
  create: 'Created', add: 'Added', set: 'Set', update: 'Updated', edit: 'Edited', delete: 'Deleted',
  remove: 'Removed', replace: 'Replaced', move: 'Moved', reorder: 'Reordered', clear: 'Cleared',
  upload: 'Uploaded', uploaded: 'Uploaded', merge: 'Merged',
};

/** Turns a change `$type` into a readable past-tense phrase (e.g. `create:remote-resource` → "Created remote resource"). */
function humanizeType(type: string): string {
  const words = type
    .replace(/Change$/, '')
    .replace(/:/g, ' ')
    .replace(/[-_]/g, ' ')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .toLowerCase()
    .trim();
  const [first, ...rest] = words.split(' ');
  const head = PAST_TENSE_VERB[first] ?? (first.charAt(0).toUpperCase() + first.slice(1));
  return [head, ...rest].join(' ');
}

function patchOpToFact(entity: SummaryEntity, op: unknown): ChangeFact | undefined {
  const path = (prop(op, 'path') as string) ?? '';
  const segments = path.split('/').filter(Boolean);
  if (!segments.length) return undefined;
  // Homograph number isn't a view field (no field config), so surface it explicitly instead of dropping to a generic patch summary.
  if (entity === 'entry' && segments[0].toLowerCase() === 'homographnumber') {
    const homograph = displayValue(prop(op, 'value'));
    return homograph === undefined ? undefined : {kind: 'setHomograph', value: homograph};
  }
  const fieldId = normalizeFieldId(entity, segments[0]);
  if (!fieldId) return undefined;
  const ws = segments[1];

  const wsAudio = ws !== undefined && isAudioWs(ws);

  const opType = ((prop(op, 'op') as string) ?? '').toLowerCase();
  // Audio fields have no readable ws code or value, so drop both and flag the fact as audio.
  if (opType === 'remove') return wsAudio ? {kind: 'clearField', entity, fieldId, audio: true} : {kind: 'clearField', entity, fieldId, ws};

  const value = displayValue(prop(op, 'value'));
  if (value === undefined) return {kind: 'changeField', entity, fieldId};
  if (wsAudio || isMediaUri(value)) return {kind: 'setField', entity, fieldId, value: '', audio: true};
  if (value === '') return {kind: 'clearField', entity, fieldId, ws};
  return {kind: 'setField', entity, fieldId, ws, value};
}

/** Humanizes a JSON-patch path segment (a property name) into a field label, e.g. `abbreviation` → "Abbreviation". */
function humanizeFieldSegment(segment: string): string {
  const spaced = segment.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
  return spaced.charAt(0).toUpperCase() + spaced.slice(1);
}

/** Like {@link patchOpToFact} but for vocab objects, which have no field-label config — the field name is humanized from the patch path. */
function patchOpToObjectField(object: ObjectKind, op: unknown): ChangeFact | undefined {
  const path = (prop(op, 'path') as string) ?? '';
  const segments = path.split('/').filter(Boolean);
  if (!segments.length) return undefined;
  const field = humanizeFieldSegment(segments[0]);
  const ws = segments[1];

  const opType = ((prop(op, 'op') as string) ?? '').toLowerCase();
  if (opType === 'remove') return {kind: 'editObjectField', object, field, ws, cleared: true};

  const value = displayValue(prop(op, 'value'));
  if (value === undefined) return {kind: 'editObjectField', object, field, ws};
  if (value === '') return {kind: 'editObjectField', object, field, ws, cleared: true};
  return {kind: 'editObjectField', object, field, ws, value};
}

/** Handlers for non-prefixed change types, keyed by `$type`. The key set is also the source of truth for {@link isHandledChangeType}. */
const TYPED_HANDLERS: Record<string, (change: unknown) => ChangeFact[]> = {
  CreateEntryChange: (c) => [{kind: 'create', entity: 'entry', label: firstAlternative(prop(c, 'lexemeForm')) ?? firstAlternative(prop(c, 'citationForm'))}],
  CreateSenseChange: (c) => [{kind: 'create', entity: 'sense', label: firstAlternative(prop(c, 'gloss')) ?? firstAlternative(prop(c, 'definition'))}],
  CreateExampleSentenceChange: (c) => {
    const sentence = prop(c, 'sentence');
    const label = firstAlternative(sentence);
    return [{kind: 'create', entity: 'example', label, audioOnly: !label && hasAudioContent(sentence)}];
  },

  // A null id clears the part of speech; otherwise the backend resolves the assigned POS name into the fact's target.
  SetPartOfSpeechChange: (c) => prop(c, 'partOfSpeechId') == null
    ? [{kind: 'clearField', entity: 'sense', fieldId: 'partOfSpeechId'}]
    : [{kind: 'changeField', entity: 'sense', fieldId: 'partOfSpeechId'}],

  AddSemanticDomainChange: (c) => [{kind: 'addItem', entity: 'sense', fieldId: 'semanticDomains', label: semanticDomainLabel(prop(c, 'semanticDomain'))}],
  RemoveSemanticDomainChange: () => [{kind: 'removeItem', entity: 'sense', fieldId: 'semanticDomains'}],
  ReplaceSemanticDomainChange: (c) => [{kind: 'replaceItem', entity: 'sense', fieldId: 'semanticDomains', label: semanticDomainLabel(prop(c, 'semanticDomain'))}],

  AddComplexFormTypeChange: (c) => [{kind: 'addItem', entity: 'entry', fieldId: 'complexFormTypes', label: labelOf(prop(prop(c, 'complexFormType'), 'name'))}],
  RemoveComplexFormTypeChange: () => [{kind: 'removeItem', entity: 'entry', fieldId: 'complexFormTypes'}],

  AddPublicationChange: (c) => [{kind: 'addItem', entity: 'entry', fieldId: 'publishIn', label: labelOf(prop(prop(c, 'publication'), 'name'))}],
  RemovePublicationChange: () => [{kind: 'removeItem', entity: 'entry', fieldId: 'publishIn'}],
  ReplacePublicationChange: (c) => [{kind: 'replaceItem', entity: 'entry', fieldId: 'publishIn', label: labelOf(prop(prop(c, 'newPublication'), 'name'))}],

  MoveSenseToEntryChange: () => [{kind: 'moveSense'}],

  AddEntryComponentChange: () => [{kind: 'componentLink', action: 'add'}],
  SetComplexFormComponentChange: () => [{kind: 'componentLink', action: 'update'}],

  AddTranslationChange: () => [{kind: 'addItem', entity: 'example', fieldId: 'translations'}],
  RemoveTranslationChange: () => [{kind: 'removeItem', entity: 'example', fieldId: 'translations'}],
  UpdateTranslationChange: () => [{kind: 'changeField', entity: 'example', fieldId: 'translations'}],
  SetFirstTranslationIdChange: () => [{kind: 'setDefaultTranslation'}],

  CreateSensePictureChange: () => [{kind: 'sensePicture', action: 'add'}],
  RemoveSensePictureChange: () => [{kind: 'sensePicture', action: 'remove'}],
  UpdateSensePictureChange: () => [{kind: 'sensePicture', action: 'update'}],
  ReorderSensePictureChange: () => [{kind: 'sensePicture', action: 'reorder'}],
  SetMainPublicationChange: () => [{kind: 'setMainPublication'}],

  CreatePartOfSpeechChange: (c) => [{kind: 'createObject', object: 'partOfSpeech', label: labelOf(prop(c, 'name'))}],
  CreateSemanticDomainChange: (c) => [{kind: 'createObject', object: 'semanticDomain', label: semanticDomainLabel(c)}],
  CreatePublicationChange: (c) => [{kind: 'createObject', object: 'publication', label: labelOf(prop(c, 'name'))}],
  CreateComplexFormType: (c) => [{kind: 'createObject', object: 'complexFormType', label: labelOf(prop(c, 'name'))}],
  CreateWritingSystemChange: (c) => [{kind: 'createObject', object: 'writingSystem', label: labelOf(prop(c, 'name'))}],
  CreateMorphTypeChange: (c) => [{kind: 'createObject', object: 'morphType', label: labelOf(prop(c, 'name'))}],
  CreateCustomViewChange: (c) => [{kind: 'createObject', object: 'customView', label: labelOf(prop(c, 'name'))}],
  EditCustomViewChange: (c) => [{kind: 'editObject', object: 'customView', label: labelOf(prop(c, 'name'))}],
};

/** The `$type`s with a purpose-built handler — exported so the coverage test can assert each is a real generated change type (a handler must not outlive the backend type it decodes). */
export const explicitlyHandledChangeTypes: readonly string[] = Object.keys(TYPED_HANDLERS);

function jsonPatchFacts(suffix: string, change: unknown): ChangeFact[] {
  const entity = MAIN_ENTITY_BY_SUFFIX[suffix];
  if (entity) {
    const operations = prop(change, 'patchDocument');
    const facts = Array.isArray(operations)
      ? operations.map((op) => patchOpToFact(entity, op)).filter((f): f is ChangeFact => f !== undefined)
      : [];
    return facts.length ? facts : [{kind: 'generic', text: humanizeType(`jsonPatch:${suffix}`)}];
  }
  const object = OBJECT_BY_SUFFIX[suffix];
  if (object) {
    const operations = prop(change, 'patchDocument');
    const facts = Array.isArray(operations)
      ? operations.map((op) => patchOpToObjectField(object, op)).filter((f): f is ChangeFact => f !== undefined)
      : [];
    return facts.length ? facts : [{kind: 'editObject', object}];
  }
  return [{kind: 'generic', text: humanizeType(`jsonPatch:${suffix}`)}];
}

function deleteFacts(suffix: string): ChangeFact[] {
  const entity = MAIN_ENTITY_BY_SUFFIX[suffix];
  if (entity) return [{kind: 'delete', entity}];
  if (suffix === 'ComplexFormComponent') return [{kind: 'componentLink', action: 'remove'}];
  const object = OBJECT_BY_SUFFIX[suffix];
  if (object) return [{kind: 'deleteObject', object}];
  return [{kind: 'generic', text: humanizeType(`delete:${suffix}`)}];
}

/** Describes everything a single change did. A JSON-patch change yields one fact per operation. */
export function describeChange(changeEntity: IChangeEntity): ChangeFact[] {
  const change = changeEntity.change;
  const type = changeType(change);

  // Media resources carry no kind in the change, and the field-set that references one lives in a separate
  // commit — so kind can't be derived here without fetching the file. Assume audio: it's the only resource
  // kind today. Revisit (mime from the media service) when non-audio resources ship.
  if (type === 'create:remote-resource') return [{kind: 'mediaResource', action: 'add', resourceId: resourceIdOf(change), audio: true}];
  if (type === 'uploaded:RemoteResource') return [{kind: 'mediaResource', action: 'upload', resourceId: resourceIdOf(change), audio: true}];
  if (type === 'delete:RemoteResource') return [{kind: 'mediaResource', action: 'delete', resourceId: resourceIdOf(change), audio: true}];
  if (type.startsWith('jsonPatch:')) return jsonPatchFacts(type.slice('jsonPatch:'.length), change);
  if (type.startsWith('delete:')) return deleteFacts(type.slice('delete:'.length));
  if (type.startsWith('SetOrderChange:')) {
    const collection = ORDER_COLLECTION[type.slice('SetOrderChange:'.length)];
    return collection ? [{kind: 'reorder', collection}] : [{kind: 'generic', text: humanizeType(type)}];
  }

  const handler = TYPED_HANDLERS[type];
  return handler ? handler(change) : [{kind: 'generic', text: humanizeType(type)}];
}

/**
 * Whether {@link describeChange} produces a purpose-built (non-`generic`) summary for a `$type`.
 * The coverage test asserts every generated `knownChangeTypes` value is handled here (or is on a
 * small allow-list of intentionally-generic types), so a new backend change type fails the test.
 */
export function isHandledChangeType(type: string): boolean {
  if (type === 'create:remote-resource' || type === 'uploaded:RemoteResource' || type === 'delete:RemoteResource') return true;
  if (type.startsWith('jsonPatch:')) {
    const suffix = type.slice('jsonPatch:'.length);
    return suffix in MAIN_ENTITY_BY_SUFFIX || suffix in OBJECT_BY_SUFFIX;
  }
  if (type.startsWith('delete:')) {
    const suffix = type.slice('delete:'.length);
    return suffix in MAIN_ENTITY_BY_SUFFIX || suffix in OBJECT_BY_SUFFIX || suffix === 'ComplexFormComponent';
  }
  if (type.startsWith('SetOrderChange:')) {
    return type.slice('SetOrderChange:'.length) in ORDER_COLLECTION;
  }
  return type in TYPED_HANDLERS;
}

/** A change fact paired with the resolved name of the entity it's about (from the activity payload). */
export interface ChangeFactWithSubject {
  fact: ChangeFact;
  /** Entry headword, "headword › gloss" for senses, or a vocab object's name. Undefined when unresolved. */
  subject?: string;
  rootEntryId?: string;
  /** Display headword of `rootEntryId`, for the entry-group header. Undefined when there's no root entry or no headword. */
  rootEntryHeadword?: string;
  /** An item the change names only by id, resolved by the backend: the part of speech set, the semantic domain removed. */
  target?: string;
}

function factsWithSubject(change: IChangeEntity, info: IActivityChangeInfo | undefined): ChangeFactWithSubject[] {
  return describeChange(change).map((fact) => ({
    fact,
    subject: info?.subject,
    rootEntryId: info?.rootEntryId,
    rootEntryHeadword: info?.rootEntryHeadword,
    target: info?.target,
  }));
}

/** All change facts across a commit, each paired with its resolved subject. `changeInfo` is parallel to `changes` by index. */
export function describeActivity(
  changes: readonly IChangeEntity[],
  changeInfo?: readonly IActivityChangeInfo[],
): ChangeFactWithSubject[] {
  return changes.flatMap((change, index) => factsWithSubject(change, changeInfo?.[index]));
}

/** Facts that share a root entry, so the headword can render once as a group header. */
export interface EntryGroup {
  /** Header text: the root entry's display headword. Undefined when the facts have no root entry or it has no headword. */
  headword?: string;
  /**
   * The root entry's own create/delete fact, promoted out of `facts`: it's the group's main event, and its
   * sentence already names the entry, so rendering it as the header line avoids repeating the headword.
   */
  lead?: ChangeFactWithSubject;
  facts: ChangeFactWithSubject[];
}

function isRootEntryLifecycle(entry: ChangeFactWithSubject): boolean {
  return (entry.fact.kind === 'create' || entry.fact.kind === 'delete') && entry.fact.entity === 'entry';
}

/**
 * Group a commit's facts by the entry tree they touch (`rootEntryId`), across the whole commit — a fact
 * rejoins its entry's group even when another entry's change interleaves. Group order is first occurrence;
 * fact order within a group is unchanged. The root entry's own create/delete becomes the group's `lead`.
 * Facts with no root entry (vocab objects, media, generic) never group — their sentences are self-naming
 * — so each stands alone in its own headerless group.
 */
export function groupByRootEntry(entries: readonly ChangeFactWithSubject[]): EntryGroup[] {
  const groups: EntryGroup[] = [];
  const byRoot = new Map<string, EntryGroup>();
  for (const entry of entries) {
    if (!entry.rootEntryId) {
      groups.push({facts: [entry]});
      continue;
    }
    let group = byRoot.get(entry.rootEntryId);
    if (!group) {
      group = {headword: entry.rootEntryHeadword || undefined, facts: []};
      byRoot.set(entry.rootEntryId, group);
      groups.push(group);
    }
    if (!group.lead && isRootEntryLifecycle(entry)) group.lead = entry;
    else group.facts.push(entry);
  }
  return groups;
}

/** The broad kind of a change, for a gutter glyph in Detailed mode (fast visual grepping down a commit). */
export type FactCategory = 'added' | 'removed' | 'changed' | 'reordered' | 'other';

export function factCategory(fact: ChangeFact): FactCategory {
  switch (fact.kind) {
    case 'create':
    case 'addItem':
    case 'createObject':
    case 'bulkCreate':
      return 'added';
    case 'delete':
    case 'deleteObject':
    case 'clearField':
    case 'removeItem':
      return 'removed';
    case 'setField':
    case 'setHomograph':
    case 'changeField':
    case 'replaceItem':
    case 'editObject':
    case 'editObjectField':
    case 'moveSense':
    case 'setDefaultTranslation':
    case 'setMainPublication':
      return 'changed';
    case 'reorder':
      return 'reordered';
    case 'componentLink':
      return fact.action === 'add' ? 'added' : fact.action === 'remove' ? 'removed' : 'changed';
    case 'sensePicture':
      return fact.action === 'add' ? 'added' : fact.action === 'remove' ? 'removed' : fact.action === 'reorder' ? 'reordered' : 'changed';
    case 'mediaResource':
      return fact.action === 'delete' ? 'removed' : 'added';
    default:
      return 'other';
  }
}

/**
 * Row-level change-kind badge for a commit: shape is the category, colour is reserved for `structural`
 * facts. Only a commit that summarizes to exactly ONE fact gets a badge — that covers single changes and
 * bulk creates. A multi-change commit (including a one-entry creation tree) has no single kind — its
 * per-fact glyphs classify each line instead — so it gets none.
 */
export type CommitBadge = {category: Exclude<FactCategory, 'other'>; structural: boolean};

export function commitBadge(summary: {entries: ChangeFactWithSubject[]; remaining: number}): CommitBadge | undefined {
  if (summary.entries.length !== 1 || summary.remaining > 0) return undefined;
  const fact = summary.entries[0].fact;
  const category = factCategory(fact);
  if (category === 'other') return undefined;
  return {category, structural: isStructuralFact(fact)};
}

// Structural = a whole entity created or destroyed (entry/sense/vocab object/import batch). An example
// sentence is content within a sense — like a field value — so its create/delete stays uncoloured.
function isStructuralFact(fact: ChangeFact): boolean {
  switch (fact.kind) {
    case 'create':
    case 'delete':
      return fact.entity !== 'example';
    case 'createObject':
    case 'deleteObject':
    case 'bulkCreate':
      return true;
    default:
      return false;
  }
}

/** Create change types whose commits collapse to a count when batched ("Created 100 semantic domains"). */
const BULK_CREATE_NOUNS: Record<string, BulkNoun> = {
  CreateEntryChange: 'entries',
  CreateSenseChange: 'senses',
  CreateExampleSentenceChange: 'examples',
  CreatePartOfSpeechChange: 'partsOfSpeech',
  CreateSemanticDomainChange: 'semanticDomains',
  CreatePublicationChange: 'publications',
  CreateComplexFormType: 'complexFormTypes',
  CreateMorphTypeChange: 'morphTypes',
  CreateWritingSystemChange: 'writingSystems',
  CreateCustomViewChange: 'customViews',
};

/** Whether every change in the commit is on the same single entry tree (its root entry resolved and identical). */
function allSameRoot(changeInfo?: readonly IActivityChangeInfo[]): boolean {
  const root = changeInfo?.[0]?.rootEntryId;
  return !!root && !!changeInfo && changeInfo.every((ci) => ci.rootEntryId === root);
}

/**
 * Recognises a commit (or a group of a commit's changes) that builds ONE entry tree — an entry's creation
 * plus that entry's own senses/fields ("Created entry X"), or one sense's creation plus its examples
 * ("Added sense X"). Returns that one lead fact, or null. The detail pane titles its collapsed tree cards
 * with this; the activity LIST never collapses these — it renders them grouped under the entry header
 * (see {@link groupByRootEntry}), identical to how the same tree reads inside a bigger commit.
 */
export function recognizeTreeCommit(
  changes: readonly IChangeEntity[],
  changeInfo: readonly IActivityChangeInfo[] | undefined,
  changeTypes: readonly string[],
): ChangeFactWithSubject | null {
  if (changes.length <= 1) return null; // a single change renders fine on its own
  // Building one entry (its creation + that entry's own senses/fields) → "Created entry X".
  if (changeTypes.includes('CreateEntryChange') && allSameRoot(changeInfo)) {
    const index = changes.findIndex((c) => changeType(c.change) === 'CreateEntryChange');
    const info = changeInfo?.[index >= 0 ? index : 0];
    return {fact: {kind: 'create', entity: 'entry', label: info?.subject}, subject: info?.subject, rootEntryId: info?.rootEntryId};
  }
  // Adding one sense to an existing entry (its creation + that sense's examples) → "Added sense X".
  // Require a PURE sense-tree commit (only the sense + its examples); a mixed commit (sense + an edit) stays listed so nothing is hidden.
  if (changeTypes.includes('CreateSenseChange')
    && changeTypes.every((t) => t === 'CreateSenseChange' || t === 'CreateExampleSentenceChange')
    && allSameRoot(changeInfo)) {
    const index = changes.findIndex((c) => changeType(c.change) === 'CreateSenseChange');
    const info = changeInfo?.[index >= 0 ? index : 0];
    // Sense-create subject is the parent entry headword; target is the sense identifier (SenseGlossPart).
    // Pass target through so ChangeSummary's sense-create branch can render "<headword> · Added sense <senseLabel>".
    return {fact: {kind: 'create', entity: 'sense', label: info?.target}, subject: info?.subject, target: info?.target, rootEntryId: info?.rootEntryId};
  }
  return null;
}

/**
 * Recognises a whole commit of same-type creations across many entities (import / sync batch) —
 * "Created N entries" — without parsing every change. Driven by cheap signals the commit already carries
 * (distinct change-type keys, per-change root ids, count), so a 100-change sync commit costs nothing to classify.
 */
export function recognizeBulkCreate(
  changes: readonly IChangeEntity[],
  changeInfo: readonly IActivityChangeInfo[] | undefined,
  changeTypes: readonly string[],
): ChangeFactWithSubject | null {
  if (changes.length <= 1) return null;
  const noun = changeTypes.length === 1 ? BULK_CREATE_NOUNS[changeTypes[0]] : undefined;
  if (noun && !allSameRoot(changeInfo)) {
    return {fact: {kind: 'bulkCreate', noun, count: changes.length}};
  }
  return null;
}

/**
 * Facts for as many leading changes as fit the fact budget, plus how many changes were left off (for
 * "+N more changes"). Changes are whole units: the change that fills the budget still contributes all its
 * facts (so a many-op patch is never half-rendered), and `remaining` counts entire unrendered changes.
 */
export function describeActivityCapped(
  changes: readonly IChangeEntity[],
  changeInfo: readonly IActivityChangeInfo[] | undefined,
  maxFacts: number,
): {entries: ChangeFactWithSubject[]; remaining: number} {
  const entries: ChangeFactWithSubject[] = [];
  let consumed = 0;
  while (consumed < changes.length && entries.length < maxFacts) {
    entries.push(...factsWithSubject(changes[consumed], changeInfo?.[consumed]));
    consumed++;
  }
  return {entries, remaining: changes.length - consumed};
}

// Fact budget per commit row before the rest collapse to "+N more changes". Small — the row identifies a
// commit, the detail pane inspects it. Each fact line is CSS-clamped to 2 lines (ActivityView's factLine),
// so this budget bounds row height without measuring rendered text.
export const ROW_FACT_CAP = 6;

/**
 * Bounded summary of a commit for one row of the activity list. A bulk create collapses to a single counted
 * line; any other commit lists its leading changes until the fact budget fills, with the rest counted — an
 * entry- or sense-creation commit is NOT collapsed, so it renders grouped under its entry header exactly
 * like the same tree inside a bigger commit. Never parses more changes than it shows.
 */
export function summarizeActivity(
  changes: readonly IChangeEntity[],
  changeInfo: readonly IActivityChangeInfo[] | undefined,
  changeTypes: readonly string[],
): {entries: ChangeFactWithSubject[]; remaining: number} {
  const bulk = recognizeBulkCreate(changes, changeInfo, changeTypes);
  if (bulk) return {entries: [bulk], remaining: 0};
  return describeActivityCapped(changes, changeInfo, ROW_FACT_CAP);
}
