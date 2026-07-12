// Deliberately not shown in a vocab diff: identity/soft-delete plumbing and internal ordering/flags with no
// user meaning. The handled set is NOT listed here — vocab-diff-coverage.test.ts parses the actual
// `present('…')` rows out of DiffVocabPrimitive.svelte, so a dropped row fails the test by itself.
export const IGNORED_VOCAB_FIELDS: ReadonlySet<string> = new Set([
  'id', 'deletedAt', 'predefined', 'secondaryOrder',
]);
