import type {IEntry, ISense} from '$lib/dotnet-types';

/**
 * Ordered strongest to weakest. Candidates the backend's full-text search returned but that
 * don't match one of these — a typed vernacular value that only hit a gloss, or a typed gloss
 * that only hit a headword — are dropped rather than shown as a vague "related" hit: for a
 * duplicate warning those cross-field coincidences are noise, not signal.
 */
export type DuplicateCheckMatchKind = 'same-word' | 'similar-word' | 'similar-meaning';

/**
 * Which field an exact match hit: 'headword' when the value matched the displayed headword
 * (citation form, or a morph-token-decorated lexeme), 'lexeme' when it matched only the bare
 * lexeme form while the headword differs. Same severity either way; it only exists so the badge
 * doesn't claim "Same headword" for a lexeme-only match.
 */
export type SameWordField = 'headword' | 'lexeme';

/** Minimal slice of WritingSystemsService, so unit-testable. */
export interface DuplicateCheckWritingSystems {
  vernacularNoAudio: readonly {wsId: string}[];
  analysisNoAudio: readonly {wsId: string}[];
  /** Displayed headword for a writing system: citation form, else the morph-token-decorated lexeme. */
  headword(entry: IEntry, wsId: string): string | undefined;
}

export interface DuplicateCheckMatch {
  entry: IEntry;
  kind: DuplicateCheckMatchKind;
  field?: SameWordField;
}

export interface VernacularDuplicateCheckQuery {
  /** Diacritic-stripped, case-folded for most forgiving backend results. */
  bare: string;
  /** Case-folded, with diacritics kept, used to decide the exact same-word match. */
  exact: string;
  /** The writing system the text was typed in — used to rank that WS's headword matches first */
  wsId: string;
}

export interface DuplicateCheckQueries {
  /** Texts the user typed into vernacular fields (lexeme form, citation form) */
  vernacular: VernacularDuplicateCheckQuery[];
  /** Texts the user typed into gloss fields */
  analysis: string[];
}

export function duplicateResultContainerClass(hasExactWordMatch: boolean): string {
  return hasExactWordMatch
    ? 'border-amber-600/40 bg-amber-500/10 dark:border-amber-400/40'
    : 'border-border bg-muted/50';
}

/** Hosts submit on Enter; interacting with duplicate UI must never also create the entry. */
export function trapEnter(event: KeyboardEvent): void {
  if (event.key === 'Enter') event.stopPropagation();
}

export const MAX_SIMILAR_LENGTH_DELTA = 3;

/**
 * Deliberately broader than a starts-with headword match: mid-word containment must count
 * (typing "liman" has to surface an existing "naliman"), so it's containment in either
 * direction with no positional threshold. The simple length-delta cap keeps short fragments from
 * matching half the lexicon (typing "uz" must not surface every word containing it).
 */
export function isSimilarWord(a: string, b: string): boolean {
  if (!a || !b) return false;
  const [shorter, longer] = a.length <= b.length ? [a, b] : [b, a];
  // small words get a smaller allowance (their own length), so 'a' matches 'an' but not 'and'
  const maxDelta = Math.min(shorter.length, MAX_SIMILAR_LENGTH_DELTA);
  return longer.length - shorter.length <= maxDelta && longer.includes(shorter);
}

const kindRank: Record<DuplicateCheckMatchKind, number> = {
  'same-word': 0,
  'similar-word': 1,
  'similar-meaning': 2,
};

export function normalizeForLooseCompare(value: string | undefined): string {
  const decomposed = normalizeForExactCompare(value).trim() ?? '';
  return decomposed.toLowerCase()
    // remove diacritics
    .replace(/\p{Mn}/gu, '');
}

export function normalizeForExactCompare(value: string | undefined): string {
  return value?.normalize('NFD').trim() ?? '';
}

export function getDuplicateCheckQueries(
  entry: Pick<IEntry, 'lexemeForm' | 'citationForm'>,
  sense: Pick<ISense, 'gloss'> | undefined,
  writingSystems: DuplicateCheckWritingSystems,
): DuplicateCheckQueries {
  const vernacular: VernacularDuplicateCheckQuery[] = [];
  for (const { wsId } of writingSystems.vernacularNoAudio) {
    for (const value of [...new Set([entry.lexemeForm?.[wsId], entry.citationForm?.[wsId]])]) {
      const text = normalizeForLooseCompare(value);
      if (!text) continue;
      const exact = normalizeForExactCompare(value);
      vernacular.push({bare: text, exact, wsId});
    }
  }

  const analysis: string[] = [];
  for (const value of writingSystems.analysisNoAudio.map(ws => sense?.gloss?.[ws.wsId])) {
    const normalized = normalizeForLooseCompare(value);
    if (!normalized) continue;
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

/**
 * Classifies each candidate against the queries, returning matches strongest-first. `candidates`
 * must be in search-relevance order: that order is kept among equally-strong matches, and similar
 * words are then ordered closest-in-length first.
 */
export function classifyDuplicateCheckResults(
  candidates: IEntry[],
  queries: DuplicateCheckQueries,
  writingSystems: DuplicateCheckWritingSystems,
): DuplicateCheckMatch[] {
  const vernacularWsIds = writingSystems.vernacularNoAudio.map(ws => ws.wsId);
  const analysisWsIds = writingSystems.analysisNoAudio.map(ws => ws.wsId);
  const vernQueries = queries.vernacular;
  const analysisQueries = queries.analysis;

  function matchesExactly(forms: string[]): boolean {
    return vernQueries.some(query => forms.some(form => normalizeForExactCompare(form) === query.exact));
  }

  function sameWordField(headwordForms: string[], lexemeForms: string[]): SameWordField | undefined {
    if (matchesExactly(headwordForms)) return 'headword';
    // a bare-lexeme hit is only 'lexeme' when the headword differs; a stem (lexeme IS the
    // headword) already matched as 'headword' above
    if (matchesExactly(lexemeForms)) return 'lexeme';
    return undefined;
  }

  function hasSimilarForm(forms: string[]): boolean {
    const normalized = forms.map(form => normalizeForLooseCompare(form));
    return vernQueries.some(query => normalized.some(form => isSimilarWord(form, query.bare)));
  }

  function hasSimilarMeaning(entry: IEntry): boolean {
    const glosses = (entry.senses ?? [])
      .flatMap(sense => analysisWsIds.map(wsId => sense.gloss?.[wsId]))
      .filter((gloss): gloss is string => !!gloss)
      .map(gloss => normalizeForLooseCompare(gloss));
    return analysisQueries.some(query => glosses.some(gloss => isSimilarWord(gloss, query)));
  }

  function classify(entry: IEntry): {kind: DuplicateCheckMatchKind, field?: SameWordField} | undefined {
    const headwordForms = vernacularWsIds.map(wsId => writingSystems.headword(entry, wsId))
      .filter((form): form is string => !!form);
    const lexemeForms = vernacularWsIds.map((wsId => entry.lexemeForm?.[wsId]))
      .filter((form): form is string => !!form);

    const field = sameWordField(headwordForms, lexemeForms);
    if (field) return {kind: 'same-word', field};

    if (hasSimilarForm([...headwordForms, ...lexemeForms]))
        return {kind: 'similar-word'};

    if (hasSimilarMeaning(entry))
        return {kind: 'similar-meaning'};
    return undefined;
  }

  return candidates
    .map(entry => ({entry, match: classify(entry)}))
    .filter((candidate): candidate is {entry: IEntry, match: NonNullable<ReturnType<typeof classify>>} => candidate.match !== undefined)
    .sort((a, b) => (kindRank[a.match.kind] - kindRank[b.match.kind]))
    .map(({entry, match}) => ({entry, kind: match.kind, field: match.field}));
}
