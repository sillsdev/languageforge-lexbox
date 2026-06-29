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

/**
 * A localization-free description of one thing that happened in a change. The render component
 * ({@link ChangeSummary.svelte}) turns these into localized sentences. Kept pure so it's unit-testable.
 * Labels are pulled from the change payload when free; an id-only change carries no label (we don't
 * resolve names per row — that would be a DB hit per activity).
 */
export type ChangeFact =
  | {kind: 'create'; entity: SummaryEntity; label?: string}
  | {kind: 'delete'; entity: SummaryEntity}
  | {kind: 'setField'; entity: SummaryEntity; fieldId: FieldId; ws?: string; value: string}
  | {kind: 'clearField'; entity: SummaryEntity; fieldId: FieldId; ws?: string}
  | {kind: 'changeField'; entity: SummaryEntity; fieldId: FieldId}
  | {kind: 'addItem'; entity: SummaryEntity; fieldId: FieldId; label?: string}
  | {kind: 'removeItem'; entity: SummaryEntity; fieldId: FieldId}
  | {kind: 'replaceItem'; entity: SummaryEntity; fieldId: FieldId; label?: string}
  | {kind: 'reorder'; collection: CollectionKind}
  | {kind: 'moveSense'}
  | {kind: 'componentLink'; action: 'add' | 'update' | 'remove'}
  | {kind: 'setDefaultTranslation'}
  | {kind: 'createObject'; object: ObjectKind; label?: string}
  | {kind: 'editObject'; object: ObjectKind}
  // A decoded field edit on a vocab object. `field` is humanized from the patch path (vocab objects have no field-label config).
  | {kind: 'editObjectField'; object: ObjectKind; field: string; ws?: string; value?: string; cleared?: boolean}
  | {kind: 'deleteObject'; object: ObjectKind}
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
  if (Array.isArray(spans)) return spans.map((s) => String(prop(s, 'text') ?? '')).join('');
  return undefined;
}

/** First non-empty alternative of a MultiString or RichMultiString. */
function firstAlternative(multiString: unknown): string | undefined {
  if (!multiString || typeof multiString !== 'object') return undefined;
  for (const value of Object.values(multiString as Record<string, unknown>)) {
    const text = displayValue(value);
    if (text) return text;
  }
  return undefined;
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

/** Turns a change `$type` into a readable sentence-case phrase, keeping any verb prefix (e.g. `create:remote-resource` → "Create remote resource"). */
function humanizeType(type: string): string {
  const words = type
    .replace(/Change$/, '')
    .replace(/:/g, ' ')
    .replace(/[-_]/g, ' ')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .toLowerCase()
    .trim();
  return words.charAt(0).toUpperCase() + words.slice(1);
}

function patchOpToFact(entity: SummaryEntity, op: unknown): ChangeFact | undefined {
  const path = (prop(op, 'path') as string) ?? '';
  const segments = path.split('/').filter(Boolean);
  if (!segments.length) return undefined;
  const fieldId = normalizeFieldId(entity, segments[0]);
  if (!fieldId) return undefined;
  const ws = segments[1];

  const opType = ((prop(op, 'op') as string) ?? '').toLowerCase();
  if (opType === 'remove') return {kind: 'clearField', entity, fieldId, ws};

  const value = displayValue(prop(op, 'value'));
  if (value === undefined) return {kind: 'changeField', entity, fieldId};
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
  CreateExampleSentenceChange: (c) => [{kind: 'create', entity: 'example', label: firstAlternative(prop(c, 'sentence'))}],

  // A null id clears the part of speech; otherwise the backend resolves the assigned POS name into the fact's target.
  SetPartOfSpeechChange: (c) => prop(c, 'partOfSpeechId') == null
    ? [{kind: 'clearField', entity: 'sense', fieldId: 'partOfSpeechId'}]
    : [{kind: 'changeField', entity: 'sense', fieldId: 'partOfSpeechId'}],

  AddSemanticDomainChange: (c) => [{kind: 'addItem', entity: 'sense', fieldId: 'semanticDomains', label: semanticDomainLabel(prop(c, 'semanticDomain'))}],
  RemoveSemanticDomainChange: () => [{kind: 'removeItem', entity: 'sense', fieldId: 'semanticDomains'}],
  ReplaceSemanticDomainChange: (c) => [{kind: 'replaceItem', entity: 'sense', fieldId: 'semanticDomains', label: semanticDomainLabel(prop(c, 'semanticDomain'))}],

  AddComplexFormTypeChange: (c) => [{kind: 'addItem', entity: 'entry', fieldId: 'complexFormTypes', label: labelOf(prop(prop(c, 'complexFormType'), 'name'))}],
  RemoveComplexFormTypeChange: () => [{kind: 'removeItem', entity: 'entry', fieldId: 'complexFormTypes'}],
  ReplaceComplexFormTypeChange: (c) => [{kind: 'replaceItem', entity: 'entry', fieldId: 'complexFormTypes', label: labelOf(prop(prop(c, 'newComplexFormType'), 'name'))}],

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

  CreatePartOfSpeechChange: (c) => [{kind: 'createObject', object: 'partOfSpeech', label: labelOf(prop(c, 'name'))}],
  CreateSemanticDomainChange: (c) => [{kind: 'createObject', object: 'semanticDomain', label: labelOf(prop(c, 'code'))}],
  CreatePublicationChange: (c) => [{kind: 'createObject', object: 'publication', label: labelOf(prop(c, 'name'))}],
  CreateComplexFormType: (c) => [{kind: 'createObject', object: 'complexFormType', label: labelOf(prop(c, 'name'))}],
  CreateWritingSystemChange: (c) => [{kind: 'createObject', object: 'writingSystem', label: labelOf(prop(c, 'name'))}],
  CreateMorphTypeChange: (c) => [{kind: 'createObject', object: 'morphType', label: labelOf(prop(c, 'name'))}],
  CreateCustomViewChange: (c) => [{kind: 'createObject', object: 'customView', label: labelOf(prop(c, 'name'))}],
  EditCustomViewChange: (c) => [{kind: 'editObject', object: 'customView'}],
};

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
  /** An item the change names only by id, resolved by the backend: the part of speech set, the semantic domain removed. */
  target?: string;
}

/** All change facts across a commit, each paired with its resolved subject. `changeInfo` is parallel to `changes` by index. */
export function describeActivity(
  changes: readonly IChangeEntity[],
  changeInfo?: readonly IActivityChangeInfo[],
): ChangeFactWithSubject[] {
  return changes.flatMap((change, index) => {
    const info = changeInfo?.[index];
    return describeChange(change).map((fact) => ({fact, subject: info?.subject, rootEntryId: info?.rootEntryId, target: info?.target}));
  });
}

/**
 * Picks the headline fact for the compact (Simple) list view. A commit that creates an entry and only
 * touches that one entry's tree (its senses/examples) collapses to just the entry-creation — so the user
 * isn't left wondering what the "+N" changes were. Otherwise the first fact leads and `remaining` is counted.
 */
export function pickHeadline(entries: ChangeFactWithSubject[]): {entry: ChangeFactWithSubject; remaining: number} {
  const rootEntryId = entries[0]?.rootEntryId;
  const createEntry = entries.find((e) => e.fact.kind === 'create' && e.fact.entity === 'entry');
  const allSameEntry = !!rootEntryId && entries.every((e) => e.rootEntryId === rootEntryId);
  if (createEntry && allSameEntry) return {entry: createEntry, remaining: 0};
  return {entry: entries[0], remaining: entries.length - 1};
}
