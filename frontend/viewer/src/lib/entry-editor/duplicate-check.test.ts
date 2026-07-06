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

  it('treats a typed morph token like the backend: "-aji" is the same word as suffix entry "aji"', () => {
    const suffixEntry = makeEntry({lexemeForm: {seh: 'aji'}});
    const result = classifyDuplicates([suffixEntry], vernQueries('-aji'), vernWs, analysisWs, morphTypes);
    expect(result[0].kind).toBe('same-word');
  });

  it('is accent-insensitive only when the typed text has no diacritics (backend parity)', () => {
    const plain = makeEntry({lexemeForm: {seh: 'cafe'}});
    const accented = makeEntry({lexemeForm: {seh: 'café'}});
    // typed without diacritics -> accents ignored -> both are the same word
    expect(classifyDuplicates([plain, accented], vernQueries('cafe'), vernWs, analysisWs).map(m => m.kind))
      .toEqual(['same-word', 'same-word']);
    // typed with diacritics -> accents significant -> only the accented entry matches exactly
    const result = classifyDuplicates([accented, plain], vernQueries('café'), vernWs, analysisWs);
    expect(result[0]).toEqual({entry: accented, kind: 'same-word'});
    expect(result[1].kind).not.toBe('same-word');
  });

  it('classifies gloss overlap as same-meaning', () => {
    const entry = withGloss('cabana', 'house');
    expect(classifyDuplicates([entry], queries, vernWs, analysisWs)[0].kind).toBe('same-meaning');
  });

  it('falls back to related when nothing overlaps directly', () => {
    const entry = withGloss('cabana', 'dwelling');
    expect(classifyDuplicates([entry], queries, vernWs, analysisWs)[0].kind).toBe('related');
  });

  it('sorts word matches above meaning matches, preserving relevance order within a kind', () => {
    const meaning = withGloss('cabana', 'house');
    const similarA = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const similarB = makeEntry({lexemeForm: {seh: 'manyumba'}});
    const exact = makeEntry({lexemeForm: {seh: 'nyumba'}});
    const result = classifyDuplicates([meaning, similarA, similarB, exact], queries, vernWs, analysisWs);
    expect(result.map(m => m.entry.id)).toEqual([exact.id, similarA.id, similarB.id, meaning.id]);
  });

  it('never reports same-word when no vernacular text was typed', () => {
    const entry = makeEntry({lexemeForm: {seh: 'nyumba'}});
    const result = classifyDuplicates([entry], {vernacular: [], analysis: ['house']}, vernWs, analysisWs);
    expect(result[0].kind).toBe('related');
  });
});
