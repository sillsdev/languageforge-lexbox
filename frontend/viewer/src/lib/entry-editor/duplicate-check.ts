import type {IEntry, ISense} from '$lib/dotnet-types';

export const MIN_QUERY_LENGTH = 2;

/**
 * Ordered strongest to weakest. Candidates the backend's full-text search returned but that
 * don't match one of these — a typed vernacular value that only hit a gloss, or a typed gloss
 * that only hit a headword — are dropped rather than shown as a vague "related" hit: for a
 * duplicate warning those cross-field coincidences are noise, not signal.
 */
export type DuplicateMatchKind = 'same-word' | 'similar-word' | 'same-meaning';

/**
 * Which field an exact match hit: 'headword' when the value matched the displayed headword
 * (citation form, or a morph-token-decorated lexeme), 'lexeme' when it matched only the bare
 * lexeme form while the headword differs. Same severity either way; it only exists so the badge
 * doesn't claim "Same headword" for a lexeme-only match.
 */
export type SameWordField = 'headword' | 'lexeme';

export interface DuplicateMatch {
  entry: IEntry;
  kind: DuplicateMatchKind;
  field?: SameWordField;
}

export interface VernacularQuery {
  /** Diacritic-stripped, case-folded form sent to the backend search and used for the fuzzy
   *  (accent-insensitive) similar-word match. The backend only matches accent-insensitively when
   *  the query has no diacritics (MiniLcm StringExtensions.ContainsDiacriticMatch). */
  text: string;
  /** Case-folded form with diacritics kept, used to decide the exact same-word match. */
  exact: string;
  /** The writing system the text was typed in — used to rank that WS's headword matches first */
  wsId: string;
}

export interface DuplicateQueries {
  /** Texts the user typed into vernacular fields (lexeme form, citation form) */
  vernacular: VernacularQuery[];
  /** Texts the user typed into gloss fields */
  analysis: string[];
}

export function duplicateTintClass(hasExactWordMatch: boolean): string {
  return hasExactWordMatch
    ? 'border-amber-600/40 bg-amber-500/10 dark:border-amber-400/40'
    : 'border-border bg-muted/50';
}

/** Hosts submit on Enter; interacting with duplicate UI must never also create the entry. */
export function trapEnter(event: KeyboardEvent): void {
  if (event.key === 'Enter') event.stopPropagation();
}

export const MAX_SIMILAR_LENGTH_DELTA = 5;

/**
 * Deliberately broader than a starts-with headword match: mid-word containment must count
 * (typing "liman" has to surface an existing "naliman"), so it's containment in either
 * direction with no positional threshold. The length-delta cap keeps short fragments from
 * matching half the lexicon (typing "uz" must not surface every word containing it).
 */
export function isSimilarWord(a: string, b: string): boolean {
  const [shorter, longer] = a.length <= b.length ? [a, b] : [b, a];
  return longer.length - shorter.length <= MAX_SIMILAR_LENGTH_DELTA && longer.includes(shorter);
}

const kindRank: Record<DuplicateMatchKind, number> = {
  'same-word': 0,
  'similar-word': 1,
  'same-meaning': 2,
};

/**
 * Case-folds invariantly (never by the host locale) and, unless asked to keep them, strips
 * diacritics. The two modes back the two comparison tiers: keep diacritics to decide an exact
 * same-word match, drop them for the accent-insensitive similar-word match. Collation-level
 * equivalences (ß≈ss, ligatures, ICU-ignorable characters like soft hyphens) are not replicated.
 */
export function normalizeForCompare(value: string | undefined, keepDiacritics = false): string {
  const decomposed = value?.normalize('NFD').toLowerCase().trim() ?? '';
  if (keepDiacritics) return decomposed;
  return decomposed.replace(/\p{Mn}/gu, '');
}

export function getDuplicateEntryQueries(
  entry: Pick<IEntry, 'lexemeForm' | 'citationForm'>,
  sense: Pick<ISense, 'gloss'> | undefined,
  vernacularWsIds: string[],
  analysisWsIds: string[],
): DuplicateQueries {
  const vernacular: VernacularQuery[] = [];
  for (const wsId of vernacularWsIds) {
    // The same text typed in two writing systems stays two queries — each is searched sorted by
    // its own WS so that WS's headword matches rank first. Within a WS, lexeme/citation forms that
    // fold to the same form (identical, or differing only by case) collapse; accent variants are
    // kept, so each keeps its own exact same-word match.
    const seen = new Set<string>();
    for (const value of [entry.lexemeForm?.[wsId], entry.citationForm?.[wsId]]) {
      const text = normalizeForCompare(value);
      const exact = normalizeForCompare(value, true);
      if (text.length < MIN_QUERY_LENGTH || seen.has(exact)) continue;
      seen.add(exact);
      vernacular.push({text, exact, wsId});
    }
  }

  const analysis: string[] = [];
  for (const value of analysisWsIds.map(wsId => sense?.gloss?.[wsId])) {
    const normalized = normalizeForCompare(value);
    if (normalized.length < MIN_QUERY_LENGTH) continue;
    analysis.push(normalized);
  }
  return {vernacular, analysis};
}

/** Merges per-query search results into a single relevance-ordered candidate list. */
export function mergeSearchResults(results: IEntry[][]): IEntry[] {
  const seen = new Set<string>();
  return results.flat().filter(entry => {
    if (seen.has(entry.id)) return false;
    seen.add(entry.id);
    return true;
  });
}

/** The slice of WritingSystemService the classifier reads. Structural, so the classifier stays a
 *  pure function unit-testable without a live project context. */
export interface DuplicateWritingSystems {
  vernacularNoAudio: readonly {wsId: string}[];
  analysisNoAudio: readonly {wsId: string}[];
  /** Displayed headword for a writing system: citation form, else the morph-token-decorated lexeme. */
  headword(entry: IEntry, wsId: string): string | undefined;
}

/**
 * Classifies each candidate against the queries, returning matches strongest-first. `candidates`
 * must be in search-relevance order: that order is kept among equally-strong matches, and similar
 * words are then ordered closest-in-length first.
 */
export function classifyQueryResults(
  candidates: IEntry[],
  queries: DuplicateQueries,
  writingSystems: DuplicateWritingSystems,
): DuplicateMatch[] {
  const vernacularWsIds = writingSystems.vernacularNoAudio.map(ws => ws.wsId);
  const analysisWsIds = writingSystems.analysisNoAudio.map(ws => ws.wsId);
  const vernQueries = queries.vernacular.map(({exact, text}) => ({
    exact: normalizeForCompare(exact, true),
    fuzzy: normalizeForCompare(text),
  }));
  const analysisQueries = queries.analysis.map(text => normalizeForCompare(text));

  function vernacularForms(pick: (wsId: string) => string | undefined): string[] {
    return vernacularWsIds.map(pick).filter((form): form is string => !!form);
  }

  function matchesExactly(forms: string[]): boolean {
    return vernQueries.some(query => forms.some(form => normalizeForCompare(form, true) === query.exact));
  }

  function sameWordField(headwordForms: string[], lexemeForms: string[]): SameWordField | undefined {
    if (matchesExactly(headwordForms)) return 'headword';
    // a bare-lexeme hit is only 'lexeme' when the headword differs; a stem (lexeme IS the
    // headword) already matched as 'headword' above
    if (matchesExactly(lexemeForms)) return 'lexeme';
    return undefined;
  }

  function similarWordDelta(forms: string[]): number | undefined {
    const normalized = forms.map(form => normalizeForCompare(form));
    let delta = Infinity;
    for (const query of vernQueries) {
      for (const form of normalized) {
        if (isSimilarWord(form, query.fuzzy)) delta = Math.min(delta, Math.abs(form.length - query.fuzzy.length));
      }
    }
    return delta === Infinity ? undefined : delta;
  }

  function isSameMeaning(entry: IEntry): boolean {
    const glosses = (entry.senses ?? [])
      .flatMap(sense => analysisWsIds.map(wsId => sense.gloss?.[wsId]))
      .filter((gloss): gloss is string => !!gloss)
      .map(gloss => normalizeForCompare(gloss));
    return analysisQueries.some(query => glosses.some(gloss => gloss.includes(query) || query.includes(gloss)));
  }

  function classify(entry: IEntry): {kind: DuplicateMatchKind, field?: SameWordField, lengthDelta: number} | undefined {
    const headwordForms = vernacularForms(wsId => writingSystems.headword(entry, wsId));
    const lexemeForms = vernacularForms(wsId => entry.lexemeForm?.[wsId]);

    const field = sameWordField(headwordForms, lexemeForms);
    if (field) return {kind: 'same-word', field, lengthDelta: 0};

    const lengthDelta = similarWordDelta([...headwordForms, ...lexemeForms]);
    if (lengthDelta !== undefined) return {kind: 'similar-word', lengthDelta};

    if (isSameMeaning(entry)) return {kind: 'same-meaning', lengthDelta: 0};
    return undefined;
  }

  return candidates
    .map(entry => ({entry, match: classify(entry)}))
    .filter((candidate): candidate is {entry: IEntry, match: NonNullable<ReturnType<typeof classify>>} => candidate.match !== undefined)
    .sort((a, b) => (kindRank[a.match.kind] - kindRank[b.match.kind]) || (a.match.lengthDelta - b.match.lengthDelta))
    .map(({entry, match}) => ({entry, kind: match.kind, field: match.field}));
}
