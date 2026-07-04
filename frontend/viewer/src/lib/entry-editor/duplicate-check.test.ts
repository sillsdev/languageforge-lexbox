import {describe, expect, it} from 'vitest';
import type {IEntry} from '$lib/dotnet-types';
import {classifyDuplicates, duplicateQueries, mergeSearchResults, normalizeForCompare} from './duplicate-check';

const vernWs = ['seh', 'por'];
const analysisWs = ['en', 'fr'];

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

describe('normalizeForCompare', () => {
  it('ignores case and accents', () => {
    expect(normalizeForCompare('Ñumbá ')).toBe('numba');
    expect(normalizeForCompare('CAFÉ')).toBe('cafe');
  });
});

describe('duplicateQueries', () => {
  it('collects distinct vernacular and gloss texts', () => {
    const queries = duplicateQueries(
      {lexemeForm: {seh: 'nyumba', por: 'casa'}, citationForm: {seh: 'nyumba'}},
      {gloss: {en: 'house', fr: ''}},
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual(['nyumba', 'casa']);
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

  it('measures the length threshold on the normalized text', () => {
    // 'e' + combining acute is 2 chars raw but 1 char once marks are stripped
    const queries = duplicateQueries(
      {lexemeForm: {seh: 'é'}, citationForm: {}},
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
    expect(queries.vernacular).toEqual(['café']);
  });

  it('ignores values in writing systems outside the given lists', () => {
    const queries = duplicateQueries(
      {lexemeForm: {'seh-Zxxx-x-audio': 'clip.wav', seh: 'nyumba'}, citationForm: {}},
      undefined,
      vernWs,
      analysisWs,
    );
    expect(queries.vernacular).toEqual(['nyumba']);
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
  const queries = {vernacular: ['nyumba'], analysis: ['house']};

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
