import {describe, expect, it, vi} from 'vitest';
import type {IEntry} from '$lib/dotnet-types';
import {MorphTypeKind} from '$lib/dotnet-types';
import {
  classifyQueryResults,
  getDuplicateEntryQueries,
  mergeSearchResults,
  normalizeForCompare,
  trapEnter,
  type DuplicateQueries,
  type DuplicateWritingSystems,
} from './duplicate-check';

const vernWs = ['seh', 'por'];
const analysisWs = ['en', 'fr'];

function undecoratedHeadword(entry: IEntry, wsId: string): string | undefined {
  return entry.citationForm?.[wsId]?.trim() || entry.lexemeForm?.[wsId]?.trim() || undefined;
}

// Stands in for writingSystemService.headword: citation form wins, else a suffix's lexeme shows a
// leading token (e.g. "-aji"). Passed to exercise the morph-token classification paths.
function decoratedHeadword(entry: IEntry, wsId: string): string | undefined {
  const citation = entry.citationForm?.[wsId]?.trim();
  if (citation) return citation;
  const lexeme = entry.lexemeForm?.[wsId]?.trim();
  if (!lexeme) return undefined;
  return entry.morphType === MorphTypeKind.Suffix ? `-${lexeme}` : lexeme;
}

// The WritingSystemService slice classifyQueryResults reads; defaults to the undecorated headword.
function ws(headword = undecoratedHeadword): DuplicateWritingSystems {
  return {
    vernacularNoAudio: vernWs.map(wsId => ({wsId})),
    analysisNoAudio: analysisWs.map(wsId => ({wsId})),
    headword,
  };
}

function makeEntry(partial: Partial<IEntry>): IEntry {
  return {
    id: crypto.randomUUID(),
    lexemeForm: {},
    citationForm: {},
    senses: [],
    ...partial,
  } as IEntry;
}

function withGloss(lexeme: string, gloss: string): IEntry {
  return makeEntry({
    lexemeForm: {seh: lexeme},
    senses: [{gloss: {en: gloss}} as unknown as IEntry['senses'][number]],
  });
}

// Models a typed vernacular value: `exact` keeps its diacritics, `text` is the stripped form sent
// to the backend. classifyQueryResults re-normalizes both, so passing the raw text for each is fine.
function vernQueries(...texts: string[]): DuplicateQueries {
  return {vernacular: texts.map(text => ({text, exact: text, wsId: 'seh'})), analysis: []};
}

describe('normalizeForCompare', () => {
  it('ignores case and accents by default', () => {
    expect(normalizeForCompare('Ñumbá ')).toBe('numba');
    expect(normalizeForCompare('CAFÉ')).toBe('cafe');
  });

  it('keeps diacritics when asked, still folding case', () => {
    expect(normalizeForCompare('CAFÉ', true)).toBe('café'.normalize('NFD'));
    expect(normalizeForCompare('CAFÉ', true)).not.toBe('cafe');
  });

  it('folds case invariantly, not by host locale', () => {
    // must never produce Turkish dotless ı for ASCII I
    expect(normalizeForCompare('IZGARA')).toBe('izgara');
  });

  it('treats a missing value as empty', () => {
    expect(normalizeForCompare(undefined)).toBe('');
  });
});


describe('trapEnter', () => {
  // the host dialog submits on Enter; a leak here silently creates the duplicate being warned about
  it('stops Enter from reaching the dialog, letting other keys through', () => {
    const enter = new KeyboardEvent('keydown', {key: 'Enter'});
    const enterStop = vi.spyOn(enter, 'stopPropagation');
    trapEnter(enter);
    expect(enterStop).toHaveBeenCalledOnce();

    const space = new KeyboardEvent('keydown', {key: ' '});
    const spaceStop = vi.spyOn(space, 'stopPropagation');
    trapEnter(space);
    expect(spaceStop).not.toHaveBeenCalled();
  });
});

describe('getDuplicateEntryQueries', () => {
  it('collects vernacular values tagged with their writing system, and gloss texts', () => {
    const queries = getDuplicateEntryQueries(
      {lexemeForm: {seh: 'nyumba', por: 'casa'}, citationForm: {}},
      {gloss: {en: 'house', fr: ''}},
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([
      {text: 'nyumba', exact: 'nyumba', wsId: 'seh'},
      {text: 'casa', exact: 'casa', wsId: 'por'},
    ]);
    expect(queries.analysis).toEqual(['house']);
  });

  it('skips blank and too-short values', () => {
    const queries = getDuplicateEntryQueries(
      {lexemeForm: {seh: ' n '}, citationForm: {}},
      {gloss: {en: '  '}},
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([]);
    expect(queries.analysis).toEqual([]);
  });

  it('accepts a value exactly at the length threshold', () => {
    const queries = getDuplicateEntryQueries(
      {lexemeForm: {seh: 'ba'}, citationForm: {}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([{text: 'ba', exact: 'ba', wsId: 'seh'}]);
  });

  it('measures the length threshold on the normalized text', () => {
    // 'e' + combining acute is 2 chars raw but 1 char once marks are stripped
    const queries = getDuplicateEntryQueries(
      {lexemeForm: {seh: 'é'}, citationForm: {}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([]);
  });

  it('keeps the same text typed in different writing systems as separate queries', () => {
    // each search is sorted by its own WS so that WS's headword matches rank first
    const queries = getDuplicateEntryQueries(
      {lexemeForm: {seh: 'kalata', por: 'kalata'}, citationForm: {}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([
      {text: 'kalata', exact: 'kalata', wsId: 'seh'},
      {text: 'kalata', exact: 'kalata', wsId: 'por'},
    ]);
  });

  it('keeps accent variants within a writing system, sharing the stripped query but not the exact form', () => {
    // lexeme 'café' + citation 'cafe' both search the backend for 'cafe', but each keeps its own
    // exact form so the classifier can mark whichever one the user typed as the same word
    const queries = getDuplicateEntryQueries(
      {lexemeForm: {seh: 'café'}, citationForm: {seh: 'cafe'}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([
      {text: 'cafe', exact: 'café'.normalize('NFD'), wsId: 'seh'},
      {text: 'cafe', exact: 'cafe', wsId: 'seh'},
    ]);
  });

  it('collapses lexeme and citation forms that fold together (identical, or case-only difference)', () => {
    const identical = getDuplicateEntryQueries(
      {lexemeForm: {seh: 'nyumba'}, citationForm: {seh: 'nyumba'}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(identical.vernacular).toEqual([{text: 'nyumba', exact: 'nyumba', wsId: 'seh'}]);

    // 'Cafe' and 'cafe' fold to the same form, so they collapse (accent variants would not — see above)
    const caseOnly = getDuplicateEntryQueries(
      {lexemeForm: {seh: 'Cafe'}, citationForm: {seh: 'cafe'}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(caseOnly.vernacular).toEqual([{text: 'cafe', exact: 'cafe', wsId: 'seh'}]);
  });

  it('keeps a gloss query even when the same text was typed as a vernacular value', () => {
    // loanword case: lexeme 'radio' glossed 'radio' — the gloss query must survive so
    // same-meaning matches on other entries still classify
    const queries = getDuplicateEntryQueries(
      {lexemeForm: {seh: 'radio'}, citationForm: {}},
      {gloss: {en: 'radio'}},
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([{text: 'radio', exact: 'radio', wsId: 'seh'}]);
    expect(queries.analysis).toEqual(['radio']);
  });

  it('ignores values in writing systems outside the given lists', () => {
    const queries = getDuplicateEntryQueries(
      {lexemeForm: {'seh-Zxxx-x-audio': 'clip.wav', seh: 'nyumba'}, citationForm: {}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([{text: 'nyumba', exact: 'nyumba', wsId: 'seh'}]);
  });
});

describe('mergeSearchResults', () => {
  it('dedupes entries matched by multiple queries, preserving first-seen order', () => {
    const a = makeEntry({});
    const b = makeEntry({});
    const c = makeEntry({});
    expect(mergeSearchResults([[a, b], [b, c, a]])).toEqual([a, b, c]);
  });
});

describe('classifyQueryResults', () => {
  const queries: DuplicateQueries = {vernacular: [{text: 'nyumba', exact: 'nyumba', wsId: 'seh'}], analysis: ['house']};

  it('classifies exact headword matches as same-word, even via citation form or other ws', () => {
    // case-only difference still folds to the same word; accent differences are exercised below
    const byLexeme = makeEntry({lexemeForm: {seh: 'Nyumba'}});
    const byCitation = makeEntry({citationForm: {por: 'nyumba'}});
    const result = classifyQueryResults([byLexeme, byCitation], queries, ws());
    expect(result.map(m => m.kind)).toEqual(['same-word', 'same-word']);
  });

  it('classifies partial headword overlap (either direction) as similar-word', () => {
    const superstring = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const substring = makeEntry({lexemeForm: {seh: 'yumba'}});
    const result = classifyQueryResults([superstring, substring], queries, ws());
    expect(result.map(m => m.kind)).toEqual(['similar-word', 'similar-word']);
  });

  it('classifies mid-word containment as similar-word, not just starts-with', () => {
    // real user story: typing "liman" must surface the existing entry "naliman"
    const buried = makeEntry({lexemeForm: {seh: 'kumanyumba'}});
    expect(classifyQueryResults([buried], queries, ws())[0]?.kind).toBe('similar-word');
  });

  it('drops containment matches just past the length-delta cap', () => {
    // real user story: typing "uz" must not surface every word containing it;
    // 'uzembez' is delta 5 (kept, exactly at the cap) and 'uzembeza' is delta 6 (dropped)
    const atCap = makeEntry({lexemeForm: {seh: 'uzembez'}});
    const pastCap = makeEntry({lexemeForm: {seh: 'uzembeza'}});
    const result = classifyQueryResults([atCap, pastCap], vernQueries('uz'), ws());
    expect(result).toEqual([{entry: atCap, kind: 'similar-word'}]);
  });

  it('sorts similar words closest in length first', () => {
    const far = makeEntry({lexemeForm: {seh: 'kumanyumba'}});
    const near = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const result = classifyQueryResults([far, near], queries, ws());
    expect(result.map(m => m.entry.id)).toEqual([near.id, far.id]);
  });

  it('ranks an entry by its closest form when several forms are similar', () => {
    // closeViaCitation: lexeme is delta 4 but citation is delta 1 — the citation should rank it
    const closeViaCitation = makeEntry({lexemeForm: {seh: 'kunyumbaza'}, citationForm: {seh: 'nyumbaz'}});
    const middling = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const result = classifyQueryResults([middling, closeViaCitation], queries, ws());
    expect(result.map(m => m.entry.id)).toEqual([closeViaCitation.id, middling.id]);
  });

  it('matches a typed morph token against the decorated headword: "-aji" is the suffix entry "aji"', () => {
    const suffixEntry = makeEntry({lexemeForm: {seh: 'aji'}, morphType: MorphTypeKind.Suffix});
    const result = classifyQueryResults([suffixEntry], vernQueries('-aji'), ws(decoratedHeadword));
    expect(result[0]).toMatchObject({kind: 'same-word', field: 'headword'});
  });

  it('matches a typed token against the decorated headword, not against a bare citation form', () => {
    const byLexeme = makeEntry({lexemeForm: {seh: 'aji'}, morphType: MorphTypeKind.Suffix});
    // citation forms are never token-decorated, so its headword stays 'aji'
    const byCitation = makeEntry({citationForm: {seh: 'aji'}, morphType: MorphTypeKind.Suffix});
    const result = classifyQueryResults([byLexeme, byCitation], vernQueries('-aji'), ws(decoratedHeadword));
    const kindById = new Map(result.map(m => [m.entry.id, m.kind]));
    expect(kindById.get(byLexeme.id)).toBe('same-word');
    // the token can't match the bare citation as the same word; it only lands as a loose similar hit
    expect(kindById.get(byCitation.id)).toBe('similar-word');
  });

  it('matches the bare lexeme of a suffix as a lexeme hit, not a headword hit', () => {
    // typing 'aji' (no token) against suffix entry '-aji': the lexeme matched, the headword didn't
    const suffixEntry = makeEntry({lexemeForm: {seh: 'aji'}, morphType: MorphTypeKind.Suffix});
    const result = classifyQueryResults([suffixEntry], vernQueries('aji'), ws(decoratedHeadword));
    expect(result[0]).toMatchObject({kind: 'same-word', field: 'lexeme'});
  });

  it('treats an exact-diacritic match as same-word and an accent-only difference as similar-word', () => {
    const plain = makeEntry({lexemeForm: {seh: 'cafe'}});
    const accented = makeEntry({lexemeForm: {seh: 'café'}});
    // typed 'cafe': 'cafe' is the same word; 'café' matches only once accents are ignored -> similar
    const typedPlain = new Map(
      classifyQueryResults([accented, plain], vernQueries('cafe'), ws()).map(m => [m.entry.id, m.kind]));
    expect(typedPlain.get(plain.id)).toBe('same-word');
    expect(typedPlain.get(accented.id)).toBe('similar-word');
    // typed 'café': now the accented entry is the exact match; the plain one is only similar
    const typedAccented = classifyQueryResults([plain, accented], vernQueries('café'), ws());
    expect(typedAccented.find(m => m.entry.id === accented.id)).toMatchObject({kind: 'same-word', field: 'headword'});
    expect(typedAccented.find(m => m.entry.id === plain.id)?.kind).toBe('similar-word');
  });

  it('attributes a same-word match to the field that hit', () => {
    // lexeme 'fuz' + citation 'fuza': the headword shown is 'fuza', so a match on the
    // typed 'fuz' is the same entry but NOT the same headword
    const viaLexemeOnly = makeEntry({lexemeForm: {seh: 'fuz'}, citationForm: {seh: 'fuza'}});
    const viaCitation = makeEntry({lexemeForm: {seh: 'fu'}, citationForm: {seh: 'fuz'}});
    const lexemeIsHeadword = makeEntry({lexemeForm: {seh: 'fuz'}});
    const result = classifyQueryResults([viaLexemeOnly, viaCitation, lexemeIsHeadword], vernQueries('fuz'), ws());
    const fieldById = new Map(result.map(m => [m.entry.id, m.field]));
    expect(fieldById.get(viaLexemeOnly.id)).toBe('lexeme');
    expect(fieldById.get(viaCitation.id)).toBe('headword');
    expect(fieldById.get(lexemeIsHeadword.id)).toBe('headword');
  });

  it('prefers a citation-form hit over an earlier lexeme-only hit across queries', () => {
    // one entry, two typed values: 'fuz' hits only the lexeme form, 'fuza' hits the
    // citation form — the citation hit must win the field attribution
    const entry = makeEntry({lexemeForm: {seh: 'fuz'}, citationForm: {seh: 'fuza'}});
    const result = classifyQueryResults([entry], vernQueries('fuz', 'fuza'), ws());
    expect(result[0]).toEqual({entry, kind: 'same-word', field: 'headword'});
  });

  it('classifies gloss overlap as same-meaning', () => {
    const entry = withGloss('cabana', 'house');
    expect(classifyQueryResults([entry], queries, ws())[0].kind).toBe('same-meaning');
  });

  it('classifies partial gloss containment in either direction as same-meaning', () => {
    const glossContainsQuery = withGloss('cabana', 'houseboat');
    const queryContainsGloss = withGloss('cabana', 'use');
    const result = classifyQueryResults([glossContainsQuery, queryContainsGloss], queries, ws());
    expect(result.map(m => m.kind)).toEqual(['same-meaning', 'same-meaning']);
  });

  it('drops candidates that overlap in neither headword nor gloss', () => {
    // the full-text search can return an entry via a field we don't classify (e.g. definition);
    // it should be dropped, not shown as a vague match
    const entry = withGloss('cabana', 'dwelling');
    expect(classifyQueryResults([entry], queries, ws())).toEqual([]);
  });

  it('drops a candidate whose gloss equals a typed vernacular value (cross-field coincidence)', () => {
    // typing lexeme 'nyumba' must not surface an entry merely because its gloss is 'nyumba'
    const entry = withGloss('cabana', 'nyumba');
    expect(classifyQueryResults([entry], vernQueries('nyumba'), ws())).toEqual([]);
  });

  it('sorts word matches above meaning matches, preserving relevance order within a kind', () => {
    const meaning = withGloss('cabana', 'house');
    const similarA = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const similarB = makeEntry({lexemeForm: {seh: 'manyumba'}});
    const exact = makeEntry({lexemeForm: {seh: 'nyumba'}});
    const result = classifyQueryResults([meaning, similarA, similarB, exact], queries, ws());
    expect(result.map(m => m.entry.id)).toEqual([exact.id, similarA.id, similarB.id, meaning.id]);
  });

  it('never reports a headword match when no vernacular text was typed', () => {
    // gloss-only query: an entry matched purely by headword is a cross-field coincidence and dropped
    const entry = makeEntry({lexemeForm: {seh: 'nyumba'}});
    const result = classifyQueryResults([entry], {vernacular: [], analysis: ['house']}, ws());
    expect(result).toEqual([]);
  });
});
