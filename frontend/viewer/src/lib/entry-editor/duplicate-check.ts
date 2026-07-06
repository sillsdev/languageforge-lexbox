import type {IEntry, IMorphType, ISense} from '$lib/dotnet-types';

export const MIN_QUERY_LENGTH = 2;

/**
 * Ordered strongest to weakest; `related` covers candidates the backend matched in ways we
 * don't classify: cross-field hits (vernacular text matching a gloss or vice versa), matches
 * in writing systems outside the given lists, or collation equivalences we don't replicate.
 */
export type DuplicateMatchKind = 'same-word' | 'similar-word' | 'same-meaning' | 'related';

export interface DuplicateMatch {
  entry: IEntry;
  kind: DuplicateMatchKind;
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

const kindRank: Record<DuplicateMatchKind, number> = {
  'same-word': 0,
  'similar-word': 1,
  'same-meaning': 2,
  'related': 3,
};

/**
 * Mirrors the backend fold (SqlHelpers.ContainsIgnoreCaseAccents → StringExtensions):
 * invariant case fold, and diacritics are significant only when the search text itself
 * contains them — hence the keepDiacritics mode, chosen per query via hasDiacritics().
 * Collation-level equivalences (ß≈ss, ligatures) are not replicated.
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
  const seen = new Set<string>();
  const vernacular: VernacularQuery[] = [];
  for (const wsId of vernacularWsIds) {
    for (const value of [entry.lexemeForm?.[wsId], entry.citationForm?.[wsId]]) {
      const trimmed = value?.trim();
      if (!trimmed) continue;
      const normalized = normalizeForCompare(trimmed);
      if (normalized.length < MIN_QUERY_LENGTH || seen.has(normalized)) continue;
      seen.add(normalized);
      vernacular.push({text: trimmed, wsId});
    }
  }

  const analysis: string[] = [];
  for (const value of analysisWsIds.map(wsId => sense?.gloss?.[wsId])) {
    const trimmed = value?.trim();
    if (!trimmed) continue;
    const normalized = normalizeForCompare(trimmed);
    if (normalized.length < MIN_QUERY_LENGTH || seen.has(normalized)) continue;
    seen.add(normalized);
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
 * Classifies search results against what the user typed and orders them strongest match first
 * (headword matches before gloss matches). `candidates` are expected in search-relevance order,
 * which is preserved within each kind.
 *
 * Same-word is deliberately broader than the backend's headwordMatches ranking: any lexeme or
 * citation form in any given vernacular WS counts, and typed morph tokens (e.g. "-a" on a
 * suffix) are stripped the way the backend strips them before matching lexeme forms.
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
      variants: [...new Set([
        normalizeForCompare(text, keepDiacritics),
        normalizeForCompare(stripMorphTokens(text, morphTypes), keepDiacritics),
      ])],
    };
  });
  const analysisQueries = queries.analysis.map(text => ({
    keepDiacritics: hasDiacritics(text),
    text,
  }));

  function classify(entry: IEntry): DuplicateMatchKind {
    const rawForms = vernacularWsIds.flatMap(wsId => [entry.lexemeForm?.[wsId], entry.citationForm?.[wsId]])
      .filter((form): form is string => !!form);
    if (rawForms.length) {
      for (const query of vernQueries) {
        const forms = rawForms.map(form => normalizeForCompare(form, query.keepDiacritics));
        if (query.variants.some(variant => forms.includes(variant))) return 'same-word';
      }
      for (const query of vernQueries) {
        const forms = rawForms.map(form => normalizeForCompare(form, query.keepDiacritics));
        if (query.variants.some(variant => forms.some(form => form.includes(variant) || variant.includes(form)))) {
          return 'similar-word';
        }
      }
    }
    if (analysisQueries.length) {
      const rawGlosses = (entry.senses ?? [])
        .flatMap(sense => analysisWsIds.map(wsId => sense.gloss?.[wsId]))
        .filter((gloss): gloss is string => !!gloss);
      for (const query of analysisQueries) {
        const queryText = normalizeForCompare(query.text, query.keepDiacritics);
        const glosses = rawGlosses.map(gloss => normalizeForCompare(gloss, query.keepDiacritics));
        if (glosses.some(gloss => gloss.includes(queryText) || queryText.includes(gloss))) return 'same-meaning';
      }
    }
    return 'related';
  }

  return candidates
    .map(entry => ({entry, kind: classify(entry)}))
    .sort((a, b) => kindRank[a.kind] - kindRank[b.kind]);
}
