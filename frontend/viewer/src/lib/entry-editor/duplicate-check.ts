import type {IEntry, ISense} from '$lib/dotnet-types';

export const MIN_QUERY_LENGTH = 2;

/**
 * Ordered strongest to weakest; `related` covers candidates the backend matched some other
 * way (e.g. via definition text), which we still show but without a match badge.
 */
export type DuplicateMatchKind = 'same-word' | 'similar-word' | 'same-meaning' | 'related';

export interface DuplicateMatch {
  entry: IEntry;
  kind: DuplicateMatchKind;
}

export interface DuplicateQueries {
  /** Texts the user typed into vernacular fields (lexeme form, citation form) */
  vernacular: string[];
  /** Texts the user typed into gloss fields */
  analysis: string[];
}

const kindRank: Record<DuplicateMatchKind, number> = {
  'same-word': 0,
  'similar-word': 1,
  'same-meaning': 2,
  'related': 3,
};

// mirrors the backend's ContainsIgnoreCaseAccents comparisons (SqlHelpers)
export function normalizeForCompare(value: string): string {
  return value.normalize('NFD').replace(/\p{Mn}/gu, '').toLocaleLowerCase().trim();
}

function distinctQueries(values: (string | undefined)[]): string[] {
  const seen = new Set<string>();
  const queries: string[] = [];
  for (const value of values) {
    const trimmed = value?.trim();
    if (!trimmed) continue;
    const normalized = normalizeForCompare(trimmed);
    if (normalized.length < MIN_QUERY_LENGTH || seen.has(normalized)) continue;
    seen.add(normalized);
    queries.push(trimmed);
  }
  return queries;
}

export function duplicateQueries(
  entry: Pick<IEntry, 'lexemeForm' | 'citationForm'>,
  sense: Pick<ISense, 'gloss'> | undefined,
  vernacularWsIds: string[],
  analysisWsIds: string[],
): DuplicateQueries {
  return {
    vernacular: distinctQueries(vernacularWsIds.flatMap(ws => [entry.lexemeForm?.[ws], entry.citationForm?.[ws]])),
    analysis: distinctQueries(analysisWsIds.map(ws => sense?.gloss?.[ws])),
  };
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
 */
export function classifyDuplicates(
  candidates: IEntry[],
  queries: DuplicateQueries,
  vernacularWsIds: string[],
  analysisWsIds: string[],
): DuplicateMatch[] {
  const vernQueries = queries.vernacular.map(normalizeForCompare);
  const analysisQueries = queries.analysis.map(normalizeForCompare);

  function classify(entry: IEntry): DuplicateMatchKind {
    const forms = vernacularWsIds.flatMap(ws => [entry.lexemeForm?.[ws], entry.citationForm?.[ws]])
      .filter((form): form is string => !!form)
      .map(normalizeForCompare);
    if (vernQueries.length) {
      if (forms.some(form => vernQueries.includes(form))) return 'same-word';
      if (forms.some(form => vernQueries.some(query => form.includes(query) || query.includes(form)))) return 'similar-word';
    }
    if (analysisQueries.length) {
      const glosses = (entry.senses ?? [])
        .flatMap(sense => analysisWsIds.map(ws => sense.gloss?.[ws]))
        .filter((gloss): gloss is string => !!gloss)
        .map(normalizeForCompare);
      if (glosses.some(gloss => analysisQueries.some(query => gloss.includes(query) || query.includes(gloss)))) {
        return 'same-meaning';
      }
    }
    return 'related';
  }

  return candidates
    .map(entry => ({entry, kind: classify(entry)}))
    .sort((a, b) => kindRank[a.kind] - kindRank[b.kind]);
}
