/* eslint-disable @typescript-eslint/naming-convention -- keys below are backend change-type discriminators and entity-type suffixes; all are intentionally PascalCase */

/**
 * The subject a change type is about, for grouping the activity-type filter. Users think in terms of
 * WHAT was touched (a word, a meaning, an example…), not CRDT change-class names — so the filter presents
 * the ~65 generated change types as a handful of subject sections. The coverage test asserts every
 * generated change type maps to a section, so a new backend type can't silently land ungrouped.
 */
export type ChangeTypeSection =
  | 'entry'
  | 'sense'
  | 'example'
  | 'media'
  | 'comments'
  | 'vocabulary';

/** Filter display order: dictionary content first (most-filtered-for), then supporting data. */
export const CHANGE_TYPE_SECTIONS: readonly ChangeTypeSection[] = [
  'entry',
  'sense',
  'example',
  'media',
  'comments',
  'vocabulary',
];

const SUFFIX_SECTIONS: Record<string, ChangeTypeSection> = {
  Entry: 'entry',
  // A component link concerns the entries at both ends; it's an entry-level structure either way.
  ComplexFormComponent: 'entry',
  Sense: 'sense',
  ExampleSentence: 'example',
  RemoteResource: 'media',
  UserComment: 'comments',
  CommentThread: 'comments',
  PartOfSpeech: 'vocabulary',
  SemanticDomain: 'vocabulary',
  Publication: 'vocabulary',
  ComplexFormType: 'vocabulary',
  MorphType: 'vocabulary',
  WritingSystem: 'vocabulary',
  CustomView: 'vocabulary',
};

// Dedicated change classes, grouped by the entity whose content they change (a semantic-domain tag added
// to a sense is a SENSE change; creating the semantic domain itself is a vocabulary change).
const TYPED_SECTIONS: Record<string, ChangeTypeSection> = {
  CreateEntryChange: 'entry',
  AddEntryComponentChange: 'entry',
  SetComplexFormComponentChange: 'entry',
  AddComplexFormTypeChange: 'entry',
  RemoveComplexFormTypeChange: 'entry',
  AddPublicationChange: 'entry',
  RemovePublicationChange: 'entry',
  ReplacePublicationChange: 'entry',

  CreateSenseChange: 'sense',
  MoveSenseToEntryChange: 'sense',
  SetPartOfSpeechChange: 'sense',
  AddSemanticDomainChange: 'sense',
  RemoveSemanticDomainChange: 'sense',
  ReplaceSemanticDomainChange: 'sense',
  CreateSensePictureChange: 'sense',
  RemoveSensePictureChange: 'sense',
  UpdateSensePictureChange: 'sense',
  ReorderSensePictureChange: 'sense',

  CreateExampleSentenceChange: 'example',
  AddTranslationChange: 'example',
  RemoveTranslationChange: 'example',
  UpdateTranslationChange: 'example',
  SetFirstTranslationIdChange: 'example',

  CreateCommentThreadChange: 'comments',
  CreateUserCommentChange: 'comments',
  EditUserCommentChange: 'comments',
  SetCommentThreadStatusChange: 'comments',

  CreatePartOfSpeechChange: 'vocabulary',
  CreateSemanticDomainChange: 'vocabulary',
  CreatePublicationChange: 'vocabulary',
  SetMainPublicationChange: 'vocabulary',
  CreateComplexFormType: 'vocabulary',
  CreateMorphTypeChange: 'vocabulary',
  CreateWritingSystemChange: 'vocabulary',
  CreateCustomViewChange: 'vocabulary',
  EditCustomViewChange: 'vocabulary',

  'create:remote-resource': 'media',
  'create:pendingUpload': 'media',
};

/** The `$type`s with a dedicated section mapping — exported so the coverage test can assert each is a real generated change type (a mapping must not outlive the backend type it groups). */
export const explicitlyGroupedChangeTypes: readonly string[] = Object.keys(TYPED_SECTIONS);

/**
 * The section a change-type key belongs to, or undefined for a key this module doesn't know — the
 * coverage test fails on any generated type that returns undefined, so unknowns surface at build time,
 * never as a silently ungrouped filter row.
 */
export function changeTypeSection(key: string): ChangeTypeSection | undefined {
  for (const prefix of ['jsonPatch:', 'delete:', 'SetOrderChange:', 'uploaded:']) {
    if (key.startsWith(prefix)) return SUFFIX_SECTIONS[key.slice(prefix.length)];
  }
  return TYPED_SECTIONS[key];
}
