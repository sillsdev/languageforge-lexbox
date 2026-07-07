import {describe, expect, it} from 'vitest';
import type {IEntry} from '$lib/dotnet-types';
import {
  classifyDuplicates,
  duplicateQueries,
  hasDiacritics,
  mergeSearchResults,
  normalizeForCompare,
  stripMorphTokens,
  type DuplicateQueries,
} from './duplicate-check';

const vernWs = ['seh', 'por'];
const analysisWs = ['en', 'fr'];
// canonical suffix/prefix morph-token shapes (CanonicalMorphTypes)
const morphTypes = [{prefix: '-', postfix: undefined}, {prefix: undefined, postfix: '-'}];

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

function vernQueries(...texts: string[]): DuplicateQueries {
  return {vernacular: texts.map(text => ({text, wsId: 'seh'})), analysis: []};
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
});

describe('hasDiacritics', () => {
  it('detects combining marks in composed and decomposed input', () => {
    expect(hasDiacritics('café')).toBe(true);
    expect(hasDiacritics('café')).toBe(true);
    expect(hasDiacritics('cafe')).toBe(false);
  });
});

describe('stripMorphTokens', () => {
  it('strips a leading token', () => {
    expect(stripMorphTokens('-aji', morphTypes)).toBe('aji');
  });

  it('strips a trailing token', () => {
    expect(stripMorphTokens('a-', morphTypes)).toBe('a');
  });

  it('prefers the type matching a leading token over a trailing one', () => {
    // '-a-' matches both shapes; leading-token match scores higher (mirrors backend scoring)
    expect(stripMorphTokens('-a-', [{prefix: '-', postfix: '-'}])).toBe('a');
  });

  it('leaves untokenized input alone', () => {
    expect(stripMorphTokens('aji', morphTypes)).toBe('aji');
  });
});

describe('duplicateQueries', () => {
  it('collects distinct vernacular texts with their writing system, and gloss texts', () => {
    const queries = duplicateQueries(
      {lexemeForm: {seh: 'nyumba', por: 'casa'}, citationForm: {seh: 'nyumba'}},
      {gloss: {en: 'house', fr: ''}},
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([{text: 'nyumba', wsId: 'seh'}, {text: 'casa', wsId: 'por'}]);
    expect(queries.analysis).toEqual(['house']);
  });

  it('skips blank and too-short values', () => {
    const queries = duplicateQueries(
      {lexemeForm: {seh: ' n '}, citationForm: {}},
      {gloss: {en: '  '}},
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([]);
    expect(queries.analysis).toEqual([]);
  });

  it('accepts a value exactly at the length threshold', () => {
    const queries = duplicateQueries(
      {lexemeForm: {seh: 'ba'}, citationForm: {}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([{text: 'ba', wsId: 'seh'}]);
  });

  it('measures the length threshold on the normalized text', () => {
    // 'e' + combining acute is 2 chars raw but 1 char once marks are stripped
    const queries = duplicateQueries(
      {lexemeForm: {seh: 'é'}, citationForm: {}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([]);
  });

  it('dedupes values that only differ by case or accents', () => {
    const queries = duplicateQueries(
      {lexemeForm: {seh: 'café'}, citationForm: {seh: 'Cafe'}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([{text: 'café', wsId: 'seh'}]);
  });

  it('keeps a gloss query even when the same text was typed as a vernacular value', () => {
    // loanword case: lexeme 'radio' glossed 'radio' — the gloss query must survive so
    // same-meaning matches on other entries still classify
    const queries = duplicateQueries(
      {lexemeForm: {seh: 'radio'}, citationForm: {}},
      {gloss: {en: 'radio'}},
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([{text: 'radio', wsId: 'seh'}]);
    expect(queries.analysis).toEqual(['radio']);
  });

  it('ignores values in writing systems outside the given lists', () => {
    const queries = duplicateQueries(
      {lexemeForm: {'seh-Zxxx-x-audio': 'clip.wav', seh: 'nyumba'}, citationForm: {}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual([{text: 'nyumba', wsId: 'seh'}]);
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

describe('classifyDuplicates', () => {
  const queries: DuplicateQueries = {vernacular: [{text: 'nyumba', wsId: 'seh'}], analysis: ['house']};

  it('classifies exact headword matches as same-word, even via citation form or other ws', () => {
    const byLexeme = makeEntry({lexemeForm: {seh: 'Nyumbá'}});
    const byCitation = makeEntry({citationForm: {por: 'nyumba'}});
    const result = classifyDuplicates([byLexeme, byCitation], queries, vernWs, analysisWs);
    expect(result.map(m => m.kind)).toEqual(['same-word', 'same-word']);
  });

  it('classifies partial headword overlap (either direction) as similar-word', () => {
    const superstring = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const substring = makeEntry({lexemeForm: {seh: 'yumba'}});
    const result = classifyDuplicates([superstring, substring], queries, vernWs, analysisWs);
    expect(result.map(m => m.kind)).toEqual(['similar-word', 'similar-word']);
  });

  it('classifies mid-word containment as similar-word, not just starts-with', () => {
    // real user story: typing "liman" must surface the existing entry "naliman"
    const buried = makeEntry({lexemeForm: {seh: 'kumanyumba'}});
    expect(classifyDuplicates([buried], queries, vernWs, analysisWs)[0]?.kind).toBe('similar-word');
  });

  it('drops containment matches just past the length-delta cap', () => {
    // real user story: typing "uz" must not surface every word containing it;
    // 'uzembez' is delta 5 (kept, exactly at the cap) and 'uzembeza' is delta 6 (dropped)
    const atCap = makeEntry({lexemeForm: {seh: 'uzembez'}});
    const pastCap = makeEntry({lexemeForm: {seh: 'uzembeza'}});
    const result = classifyDuplicates([atCap, pastCap], vernQueries('uz'), vernWs, analysisWs);
    expect(result).toEqual([{entry: atCap, kind: 'similar-word'}]);
  });

  it('sorts similar words closest in length first', () => {
    const far = makeEntry({lexemeForm: {seh: 'kumanyumba'}});
    const near = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const result = classifyDuplicates([far, near], queries, vernWs, analysisWs);
    expect(result.map(m => m.entry.id)).toEqual([near.id, far.id]);
  });

  it('ranks an entry by its closest form when several forms are similar', () => {
    // closeViaCitation: lexeme is delta 4 but citation is delta 1 — the citation should rank it
    const closeViaCitation = makeEntry({lexemeForm: {seh: 'kunyumbaza'}, citationForm: {seh: 'nyumbaz'}});
    const middling = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const result = classifyDuplicates([middling, closeViaCitation], queries, vernWs, analysisWs);
    expect(result.map(m => m.entry.id)).toEqual([closeViaCitation.id, middling.id]);
  });

  it('treats a typed morph token like the backend: "-aji" is the same word as suffix entry "aji"', () => {
    const suffixEntry = makeEntry({lexemeForm: {seh: 'aji'}});
    const result = classifyDuplicates([suffixEntry], vernQueries('-aji'), vernWs, analysisWs, morphTypes);
    expect(result[0].kind).toBe('same-word');
  });

  it('strips morph tokens for lexeme forms but keeps them for citation forms (backend parity)', () => {
    const byLexeme = makeEntry({lexemeForm: {seh: 'aji'}});
    const byCitation = makeEntry({citationForm: {seh: 'aji'}});
    const result = classifyDuplicates([byLexeme, byCitation], vernQueries('-aji'), vernWs, analysisWs, morphTypes);
    const kindById = new Map(result.map(m => [m.entry.id, m.kind]));
    expect(kindById.get(byLexeme.id)).toBe('same-word');
    // citation form 'aji' is compared against the un-stripped '-aji', so it is not the same word
    expect(kindById.get(byCitation.id)).not.toBe('same-word');
  });

  it('is accent-insensitive only when the typed text has no diacritics (backend parity)', () => {
    const plain = makeEntry({lexemeForm: {seh: 'cafe'}});
    const accented = makeEntry({lexemeForm: {seh: 'café'}});
    // typed without diacritics -> accents ignored -> both are the same word
    expect(classifyDuplicates([plain, accented], vernQueries('cafe'), vernWs, analysisWs).map(m => m.kind))
      .toEqual(['same-word', 'same-word']);
    // typed with diacritics -> accents significant -> only the accented entry matches exactly
    const result = classifyDuplicates([accented, plain], vernQueries('café'), vernWs, analysisWs);
    expect(result[0]).toEqual({entry: accented, kind: 'same-word', field: 'headword'});
    expect(result[1].kind).not.toBe('same-word');
  });

  it('attributes a same-word match to the field that hit', () => {
    // lexeme 'fuz' + citation 'fuza': the headword shown is 'fuza', so a match on the
    // typed 'fuz' is the same entry but NOT the same headword
    const viaLexemeOnly = makeEntry({lexemeForm: {seh: 'fuz'}, citationForm: {seh: 'fuza'}});
    const viaCitation = makeEntry({lexemeForm: {seh: 'fu'}, citationForm: {seh: 'fuz'}});
    const lexemeIsHeadword = makeEntry({lexemeForm: {seh: 'fuz'}});
    const result = classifyDuplicates([viaLexemeOnly, viaCitation, lexemeIsHeadword], vernQueries('fuz'), vernWs, analysisWs);
    const fieldById = new Map(result.map(m => [m.entry.id, m.field]));
    expect(fieldById.get(viaLexemeOnly.id)).toBe('lexeme');
    expect(fieldById.get(viaCitation.id)).toBe('headword');
    expect(fieldById.get(lexemeIsHeadword.id)).toBe('headword');
  });

  it('prefers a citation-form hit over an earlier lexeme-only hit across queries', () => {
    // one entry, two typed values: 'fuz' hits only the lexeme form, 'fuza' hits the
    // citation form — the citation hit must win the field attribution
    const entry = makeEntry({lexemeForm: {seh: 'fuz'}, citationForm: {seh: 'fuza'}});
    const result = classifyDuplicates([entry], vernQueries('fuz', 'fuza'), vernWs, analysisWs);
    expect(result[0]).toEqual({entry, kind: 'same-word', field: 'headword'});
  });

  it('classifies gloss overlap as same-meaning', () => {
    const entry = withGloss('cabana', 'house');
    expect(classifyDuplicates([entry], queries, vernWs, analysisWs)[0].kind).toBe('same-meaning');
  });

  it('classifies partial gloss containment in either direction as same-meaning', () => {
    const glossContainsQuery = withGloss('cabana', 'houseboat');
    const queryContainsGloss = withGloss('cabana', 'use');
    const result = classifyDuplicates([glossContainsQuery, queryContainsGloss], queries, vernWs, analysisWs);
    expect(result.map(m => m.kind)).toEqual(['same-meaning', 'same-meaning']);
  });

  it('drops candidates that overlap in neither headword nor gloss', () => {
    // the full-text search can return an entry via a field we don't classify (e.g. definition);
    // it should be dropped, not shown as a vague match
    const entry = withGloss('cabana', 'dwelling');
    expect(classifyDuplicates([entry], queries, vernWs, analysisWs)).toEqual([]);
  });

  it('drops a candidate whose gloss equals a typed vernacular value (cross-field coincidence)', () => {
    // typing lexeme 'nyumba' must not surface an entry merely because its gloss is 'nyumba'
    const entry = withGloss('cabana', 'nyumba');
    expect(classifyDuplicates([entry], vernQueries('nyumba'), vernWs, analysisWs)).toEqual([]);
  });

  it('sorts word matches above meaning matches, preserving relevance order within a kind', () => {
    const meaning = withGloss('cabana', 'house');
    const similarA = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const similarB = makeEntry({lexemeForm: {seh: 'manyumba'}});
    const exact = makeEntry({lexemeForm: {seh: 'nyumba'}});
    const result = classifyDuplicates([meaning, similarA, similarB, exact], queries, vernWs, analysisWs);
    expect(result.map(m => m.entry.id)).toEqual([exact.id, similarA.id, similarB.id, meaning.id]);
  });

  it('never reports a headword match when no vernacular text was typed', () => {
    // gloss-only query: an entry matched purely by headword is a cross-field coincidence and dropped
    const entry = makeEntry({lexemeForm: {seh: 'nyumba'}});
    const result = classifyDuplicates([entry], {vernacular: [], analysis: ['house']}, vernWs, analysisWs);
    expect(result).toEqual([]);
  });
});
