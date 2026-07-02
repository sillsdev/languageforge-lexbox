// Source of truth for the field-completeness guardrail on DiffVocabPrimitive (see vocab-diff-coverage.test.ts).
// Every property on every vocab model (PartOfSpeech, SemanticDomain, Publication, ComplexFormType, MorphType,
// WritingSystem, CustomView) must be either HANDLED (has a row in DiffVocabPrimitive) or IGNORED (deliberately
// not shown). When a model gains a field, the test fails until it's added to the matching set — and, if handled,
// a row is added to DiffVocabPrimitive. Keep this in sync with the rows there.

export const HANDLED_VOCAB_FIELDS: ReadonlySet<string> = new Set([
  'name', 'abbreviation', 'code', 'kind', 'description', 'prefix', 'postfix',
  'wsId', 'font', 'type', 'isMain', 'isAudio', 'exemplars',
  // CustomView
  'base', 'entryFields', 'senseFields', 'exampleFields', 'vernacular', 'analysis',
]);

// Not shown in a diff: identity/soft-delete plumbing and internal ordering/flags with no user meaning.
export const IGNORED_VOCAB_FIELDS: ReadonlySet<string> = new Set([
  'id', 'deletedAt', 'predefined', 'secondaryOrder',
]);
