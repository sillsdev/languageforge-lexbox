import {describe, expect, it, vi} from 'vitest';
import type {IEntry, IWritingSystem} from '$lib/dotnet-types';
import {MorphTypeKind} from '$lib/dotnet-types';
import {
  classifyDuplicateCheckResults,
  getDuplicateCheckQueries,
  isSimilarWord,
  mergeSearchResults,
  normalizeForExactCompare,
  normalizeForLooseCompare,
  trapEnter,
  type DuplicateCheckQueries,
  type DuplicateCheckWritingSystems
} from './duplicate-check';

// The WritingSystemService slice classifyQueryResults reads; defaults to the undecorated headword.
const writingSystems: DuplicateCheckWritingSystems = Object.freeze({
  vernacularNoAudio: ['seh', 'por'].map(wsId => ({wsId}) as IWritingSystem),
  analysisNoAudio: ['en', 'fr'].map(wsId => ({wsId}) as IWritingSystem),
  headword(entry, wsId): string {
    if (!wsId) throw new Error('these tests always pass a writing system');
    const citation = entry.citationForm?.[wsId]?.trim();
    if (citation) return citation;
    const lexeme = entry.lexemeForm?.[wsId]?.trim();
    if (!lexeme) return '';
    return entry.morphType === MorphTypeKind.Suffix ? `-${lexeme}` : lexeme;
  },
});

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

// Models a typed vernacular value the way getDuplicateEntryQueries would: `exact` keeps diacritics,
// `bare` is the stripped form sent to the backend. classifyQueryResults consumes these as-is, so the
// helper normalizes here just like production does.
function vernQueries(...texts: string[]): DuplicateCheckQueries {
  return {
    vernacular: texts.map(text => ({
      bare: normalizeForLooseCompare(text),
      exact: normalizeForExactCompare(text),
      wsId: 'seh',
    })),
    analysis: [],
  };
}

function analysisQuery(gloss: string): DuplicateCheckQueries {
  return {vernacular: [], analysis: [normalizeForLooseCompare(gloss)]};
}

describe('normalizeForLooseCompare', () => {
  it('case-folds and strips diacritics', () => {
    expect(normalizeForLooseCompare('Ñumbá ')).toBe('numba');
    expect(normalizeForLooseCompare('CAFÉ')).toBe('cafe');
  });

  it('treats a missing value as empty', () => {
    expect(normalizeForLooseCompare(undefined)).toBe('');
  });
});

describe('normalizeForExactCompare', () => {
  it('only decomposes (preserves case and diacritics)', () => {
    expect(normalizeForExactCompare('Café'.normalize('NFC')))
      .toBe('Café'.normalize('NFD'));
  });

  it('treats a missing value as empty', () => {
    expect(normalizeForExactCompare(undefined)).toBe('');
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

describe('getDuplicateCheckQueries', () => {
  it('collects and normalizes query values', () => {
    const queries = getDuplicateCheckQueries(
      {lexemeForm: {seh: 'Ñyumba', por: 'casa'}, citationForm: {}},
      {gloss: {en: 'house', fr: 'Allé'}},
      writingSystems,
    );
    expect(queries.vernacular).toEqual([
      {bare: 'nyumba', exact: 'Ñyumba'.normalize('NFD'), wsId: 'seh'},
      {bare: 'casa', exact: 'casa', wsId: 'por'},
    ]);
    expect(queries.analysis).toEqual(['house', 'alle']);
  });

  it('skips blank and whitespace-only values', () => {
    const queries = getDuplicateCheckQueries(
      {lexemeForm: {seh: '   '}, citationForm: {}},
      {gloss: {en: ''}},
      writingSystems,
    );
    expect(queries.vernacular).toEqual([]);
    expect(queries.analysis).toEqual([]);
  });

  it('keeps short values (no minimum length)', () => {
    const queries = getDuplicateCheckQueries(
      {lexemeForm: {seh: 'a'}, citationForm: {}},
      undefined,
      writingSystems,
    );
    expect(queries.vernacular).toEqual([{bare: 'a', exact: 'a', wsId: 'seh'}]);
  });

  it('only keeps non audio WS\'s', () => {
    const queries = getDuplicateCheckQueries(
      {lexemeForm: {'seh-Zxxx-x-audio': 'clip.wav', seh: 'nyumba'}, citationForm: {}},
      undefined,
      writingSystems,
    );
    expect(queries.vernacular).toEqual([{bare: 'nyumba', exact: 'nyumba', wsId: 'seh'}]);
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

describe('isSimilarWord', () => {
  it('matches containment in either direction', () => {
    expect(isSimilarWord('abc', 'abcde')).toBe(true);
    expect(isSimilarWord('abcde', 'abc')).toBe(true);
    expect(isSimilarWord('abc', 'xabcx')).toBe(true);
    expect(isSimilarWord('abc', 'abd')).toBe(false);
  });

  it('allows the longer word at most 3 extra characters', () => {
    expect(isSimilarWord('abc', 'abcdef')).toBe(true);
    expect(isSimilarWord('abc', 'abcdefg')).toBe(false);
  });

  it('allows short words even fewer extra characters: their own length', () => {
    expect(isSimilarWord('a', 'an')).toBe(true);
    expect(isSimilarWord('a', 'and')).toBe(false);
    expect(isSimilarWord('uz', 'uzem')).toBe(true);
    expect(isSimilarWord('uz', 'uzemb')).toBe(false);
  });

  it('never matches an empty value', () => {
    expect(isSimilarWord('', 'abc')).toBe(false);
    expect(isSimilarWord('abc', '')).toBe(false);
  });
});

describe('classifyQueryResults', () => {

  it('classifies exact headword matches as same-word, even via lexeme form or in other ws', () => {
    const byLexeme = makeEntry({lexemeForm: {seh: 'nyumba'}});
    const byCitation = makeEntry({citationForm: {por: 'nyumba'}});
    const result = classifyDuplicateCheckResults([byLexeme, byCitation], vernQueries('nyumba'), writingSystems);
    expect(result.map(m => m.kind)).toEqual(['same-word', 'same-word']);
  });

  it('classifies partial headword overlap as similar-word', () => {
    const superstring = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const result = classifyDuplicateCheckResults([superstring], vernQueries('nyumba'), writingSystems);
    expect(result.map(m => m.kind)).toEqual(['similar-word']);
  });

  it('classifies mid-word containment as similar-word, not just starts-with', () => {
    // real user story: typing "liman" must surface the existing entry "naliman"
    const buried = makeEntry({lexemeForm: {seh: 'naliman'}});
    expect(classifyDuplicateCheckResults([buried], vernQueries('liman'), writingSystems)[0]?.kind).toBe('similar-word');
  });

  it('matches a typed morph token against the decorated headword: "-aji" is the suffix entry "aji"', () => {
    const suffixEntry = makeEntry({lexemeForm: {seh: 'aji'}, morphType: MorphTypeKind.Suffix});
    const result = classifyDuplicateCheckResults([suffixEntry], vernQueries('-aji'), writingSystems);
    expect(result[0]).toMatchObject({kind: 'same-word', field: 'headword'});
  });

  it('matches a typed token against the decorated headword', () => {
    // typing '-aji' (with token) against suffix entry '-aji'
    const byLexeme = makeEntry({lexemeForm: {seh: 'aji'}, morphType: MorphTypeKind.Suffix});
    const byCitation = makeEntry({citationForm: {seh: '-aji'}, morphType: MorphTypeKind.Suffix});
    const result = classifyDuplicateCheckResults([byLexeme, byCitation], vernQueries('-aji'), writingSystems);
    expect(result[0]).toMatchObject({kind: 'same-word', field: 'headword'});
    expect(result[1]).toMatchObject({kind: 'same-word', field: 'headword'});
  });

  it('reports a suffix typed without its token as same-word, attributed to the lexeme', () => {
    // typing 'aji' against suffix entry with headword '-aji': the headword only matched loosely,
    // but the bare lexeme matched exactly — that's still same-word, just not a headword hit
    const suffixEntry = makeEntry({lexemeForm: {seh: 'aji'}, morphType: MorphTypeKind.Suffix});
    const result = classifyDuplicateCheckResults([suffixEntry], vernQueries('aji'), writingSystems);
    expect(result[0]).toMatchObject({kind: 'same-word', field: 'lexeme'});
  });

  it('treats an exact-diacritic match as same-word and an accent-only difference as similar-word', () => {
    const plain = makeEntry({lexemeForm: {seh: 'cafe'}});
    const accented = makeEntry({lexemeForm: {seh: 'café'}});
    // typed 'cafe': 'cafe' is the same word; 'café' matches only once accents are ignored -> similar
    const typedPlain = new Map(
      classifyDuplicateCheckResults([accented, plain], vernQueries('cafe'), writingSystems).map(m => [m.entry.id, m.kind]));
    expect(typedPlain.get(plain.id)).toBe('same-word');
    expect(typedPlain.get(accented.id)).toBe('similar-word');
    // typed 'café': now the accented entry is the exact match; the plain one is only similar
    const typedAccented = new Map(
      classifyDuplicateCheckResults([plain, accented], vernQueries('café'), writingSystems).map(m => [m.entry.id, m.kind]));
    expect(typedAccented.get(accented.id)).toBe('same-word');
    expect(typedAccented.get(plain.id)).toBe('similar-word');
  });

  it('reports an exact lexeme hit as same-word even when it only loosely matches the citation form', () => {
    // lexeme 'fuz' + citation 'fuza': the headword shown is 'fuza', so a match on the
    // typed 'fuz' is the same entry but NOT the same headword
    const viaLexemeOnly = makeEntry({lexemeForm: {seh: 'fuz'}, citationForm: {seh: 'fuza'}});
    const result = classifyDuplicateCheckResults([viaLexemeOnly], vernQueries('fuz'), writingSystems);
    expect(result[0]).toMatchObject({kind: 'same-word', field: 'lexeme'});
  });

  it('prefers a citation-form hit over an earlier lexeme-only hit across queries', () => {
    // one entry, two typed values: 'fuz' hits only the lexeme form, 'fuza' hits the
    // citation form — the citation hit must win the field attribution
    const entry = makeEntry({lexemeForm: {seh: 'fuz'}, citationForm: {seh: 'fuza'}});
    const result = classifyDuplicateCheckResults([entry], vernQueries('fuz', 'fuza'), writingSystems);
    expect(result[0]).toEqual({entry, kind: 'same-word', field: 'headword'});
  });

  it('classifies gloss overlap as similar-meaning', () => {
    const entry = withGloss('cabana', 'houses');
    expect(classifyDuplicateCheckResults([entry], analysisQuery('house'), writingSystems)[0].kind).toBe('similar-meaning');
  });

  it('drops a candidate whose gloss equals a typed vernacular value (cross-field coincidence)', () => {
    // typing lexeme 'nyumba' must not surface an entry merely because its gloss is 'nyumba'
    const typedEntry = {lexemeForm: {seh: 'nyumba'}, citationForm: {}};
    const queries = getDuplicateCheckQueries(typedEntry, undefined, writingSystems);
    const candidate = withGloss('cabana', 'nyumba');
    expect(classifyDuplicateCheckResults([candidate], queries, writingSystems)).toEqual([]);
  });

  it('sorts word matches above meaning matches, preserving relevance order within a kind', () => {
    const meaning = withGloss('cabana', 'house');
    const similarA = makeEntry({lexemeForm: {seh: 'nyumbazi'}});
    const similarB = makeEntry({lexemeForm: {seh: 'manyumba'}});
    const exact = makeEntry({lexemeForm: {seh: 'nyumba'}});
    const queries = {
      vernacular: [{bare: 'nyumba', exact: 'nyumba', wsId: 'seh'}],
      analysis: ['house'],
    };
    const result = classifyDuplicateCheckResults([meaning, similarA, similarB, exact], queries, writingSystems);
    expect(result.map(m => m.entry.id)).toEqual([exact.id, similarA.id, similarB.id, meaning.id]);
  });
});
