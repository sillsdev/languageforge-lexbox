import type {IEntry, IMorphType, ISense} from '$lib/dotnet-types';

export const MIN_QUERY_LENGTH = 2;

/**
 * Ordered strongest to weakest. Candidates the backend's full-text search returned but that
 * don't match one of these — a typed vernacular value that only hit a gloss, or a typed gloss
 * that only hit a headword — are dropped rather than shown as a vague "related" hit: for a
 * duplicate warning those cross-field coincidences are noise, not signal.
 */
export type DuplicateMatchKind = 'same-word' | 'similar-word' | 'same-meaning';

/**
 * Which field an exact match hit: 'headword' when the citation form matched (or the lexeme
 * form of an entry with no citation form — the lexeme IS the headword then), 'lexeme' when
 * only the lexeme form matched while a different citation form exists. Same severity either
 * way; it only exists so the badge doesn't claim "Same headword" for a lexeme-only match.
 */
export type SameWordField = 'headword' | 'lexeme';

export interface DuplicateMatch {
  entry: IEntry;
  kind: DuplicateMatchKind;
  field?: SameWordField;
}

export interface VernacularQuery {
  text: string;
  /** The writing system the text was typed in — used to rank that WS's headword matches first */
  wsId: string;
}

export interface DuplicateQueries {
  /** Texts the user typed into vernacular fields (lexeme form, citation form) */
  vernacular: VernacularQuery[];
  /** Texts the user typed into gloss fields */
  analysis: string[];
}

export type MorphTokenSource = Pick<IMorphType, 'prefix' | 'postfix'>;

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
 * Mirrors the backend fold (SqlHelpers.ContainsIgnoreCaseAccents → StringExtensions):
 * invariant case fold, and diacritics are significant only when the search text itself
 * contains them — hence the keepDiacritics mode, chosen per query via hasDiacritics().
 * Collation-level equivalences (ß≈ss, ligatures, ICU-ignorable characters like soft hyphens)
 * are not replicated.
 */
export function normalizeForCompare(value: string, keepDiacritics = false): string {
  const decomposed = value.normalize('NFD');
  const folded = keepDiacritics ? decomposed : decomposed.replace(/\p{Mn}/gu, '');
  return folded.toLowerCase().trim();
}

export function hasDiacritics(value: string): boolean {
  return /\p{Mn}/u.test(value.normalize('NFD'));
}

/** Mirrors the backend's EntrySearchService.StripMorphTokens best-match strip. */
export function stripMorphTokens(value: string, morphTypes: readonly MorphTokenSource[]): string {
  let bestScore = 0;
  let bestMatch: MorphTokenSource | undefined;
  for (const morphType of morphTypes) {
    const prefix = morphType.prefix?.trim() ? morphType.prefix : undefined;
    const postfix = morphType.postfix?.trim() ? morphType.postfix : undefined;
    const matchesPrefix = !!prefix && value.startsWith(prefix);
    const matchesPostfix = !!postfix && value.endsWith(postfix)
      && (!matchesPrefix || value.length >= prefix.length + postfix.length);
    const score = (matchesPrefix ? 2 : 0) + (matchesPostfix ? 1 : 0);
    if (score > bestScore) {
      bestScore = score;
      bestMatch = morphType;
    }
  }
  if (!bestMatch) return value;
  let result = value;
  if (bestMatch.prefix?.trim() && result.startsWith(bestMatch.prefix)) result = result.slice(bestMatch.prefix.length);
  if (bestMatch.postfix?.trim() && result.endsWith(bestMatch.postfix)) result = result.slice(0, -bestMatch.postfix.length);
  return result;
}

export function duplicateQueries(
  entry: Pick<IEntry, 'lexemeForm' | 'citationForm'>,
  sense: Pick<ISense, 'gloss'> | undefined,
  vernacularWsIds: string[],
  analysisWsIds: string[],
): DuplicateQueries {
  // dedupe per field kind, not across: the same text typed as both lexeme and gloss must
  // still produce an analysis query, or its same-meaning matches are never classified
  const seenVernacular = new Set<string>();
  const vernacular: VernacularQuery[] = [];
  for (const wsId of vernacularWsIds) {
    for (const value of [entry.lexemeForm?.[wsId], entry.citationForm?.[wsId]]) {
      const trimmed = value?.trim();
      if (!trimmed) continue;
      const normalized = normalizeForCompare(trimmed);
      if (normalized.length < MIN_QUERY_LENGTH || seenVernacular.has(normalized)) continue;
      seenVernacular.add(normalized);
      vernacular.push({text: trimmed, wsId});
    }
  }

  const seenAnalysis = new Set<string>();
  const analysis: string[] = [];
  for (const value of analysisWsIds.map(wsId => sense?.gloss?.[wsId])) {
    const trimmed = value?.trim();
    if (!trimmed) continue;
    const normalized = normalizeForCompare(trimmed);
    if (normalized.length < MIN_QUERY_LENGTH || seenAnalysis.has(normalized)) continue;
    seenAnalysis.add(normalized);
    analysis.push(trimmed);
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

/**
 * Classifies search results against what the user typed, drops cross-field coincidences, and
 * orders survivors strongest match first (headword matches before gloss matches; similar words
 * closest in length first). `candidates` are expected in search-relevance order, which is
 * preserved between equally-strong matches.
 *
 * Match rules mirror the CRDT FTS backend (EntrySearchService): a typed value counts against a
 * lexeme or citation form in any vernacular WS, but typed morph tokens (e.g. the "-" on a suffix)
 * are stripped before comparing against lexeme forms and kept when comparing against citation
 * forms. The FwData search and the backend's short-query (< 3 chars) fallback never strip morph
 * tokens — that only affects which candidates arrive, not how they're classified here.
 */
export function classifyDuplicates(
  candidates: IEntry[],
  queries: DuplicateQueries,
  vernacularWsIds: string[],
  analysisWsIds: string[],
  morphTypes: readonly MorphTokenSource[] = [],
): DuplicateMatch[] {
  const vernQueries = queries.vernacular.map(({text}) => {
    const keepDiacritics = hasDiacritics(text);
    return {
      keepDiacritics,
      lexeme: normalizeForCompare(stripMorphTokens(text, morphTypes), keepDiacritics),
      citation: normalizeForCompare(text, keepDiacritics),
    };
  });
  const analysisQueries = queries.analysis.map(text => ({
    keepDiacritics: hasDiacritics(text),
    text,
  }));

  function classify(entry: IEntry): {kind: DuplicateMatchKind, field?: SameWordField, lengthDelta: number} | undefined {
    const lexemeForms = vernacularWsIds.map(wsId => entry.lexemeForm?.[wsId]).filter((form): form is string => !!form);
    const citationForms = vernacularWsIds.map(wsId => entry.citationForm?.[wsId]).filter((form): form is string => !!form);
    if (lexemeForms.length || citationForms.length) {
      let sameWordField: SameWordField | undefined;
      for (const query of vernQueries) {
        const lex = lexemeForms.map(form => normalizeForCompare(form, query.keepDiacritics));
        const cit = citationForms.map(form => normalizeForCompare(form, query.keepDiacritics));
        if (cit.includes(query.citation)) {
          sameWordField = 'headword';
          break;
        }
        if (lex.includes(query.lexeme)) sameWordField ??= citationForms.length ? 'lexeme' : 'headword';
      }
      if (sameWordField) return {kind: 'same-word', field: sameWordField, lengthDelta: 0};
      let lengthDelta = Infinity;
      for (const query of vernQueries) {
        const lex = lexemeForms.map(form => normalizeForCompare(form, query.keepDiacritics));
        const cit = citationForms.map(form => normalizeForCompare(form, query.keepDiacritics));
        for (const {form, queryText} of [
          ...lex.map(form => ({form, queryText: query.lexeme})),
          ...cit.map(form => ({form, queryText: query.citation})),
        ]) {
          if (isSimilarWord(form, queryText)) {
            lengthDelta = Math.min(lengthDelta, Math.abs(form.length - queryText.length));
          }
        }
      }
      if (lengthDelta !== Infinity) return {kind: 'similar-word', lengthDelta};
    }
    if (analysisQueries.length) {
      const rawGlosses = (entry.senses ?? [])
        .flatMap(sense => analysisWsIds.map(wsId => sense.gloss?.[wsId]))
        .filter((gloss): gloss is string => !!gloss);
      for (const query of analysisQueries) {
        const queryText = normalizeForCompare(query.text, query.keepDiacritics);
        const glosses = rawGlosses.map(gloss => normalizeForCompare(gloss, query.keepDiacritics));
        if (glosses.some(gloss => gloss.includes(queryText) || queryText.includes(gloss))) {
          return {kind: 'same-meaning', lengthDelta: 0};
        }
      }
    }
    return undefined;
  }

  return candidates
    .map(entry => ({entry, match: classify(entry)}))
    .filter((candidate): candidate is {entry: IEntry, match: NonNullable<ReturnType<typeof classify>>} => candidate.match !== undefined)
    .sort((a, b) => (kindRank[a.match.kind] - kindRank[b.match.kind]) || (a.match.lengthDelta - b.match.lengthDelta))
    .map(({entry, match}) => ({entry, kind: match.kind, field: match.field}));
}
