import {describe, expect, it} from 'vitest';
import {MorphTypeKind, ViewBase} from '$lib/dotnet-types';
import {AFFIXES, CLITICS, WORDS_AND_PHRASES} from '$project/data/morph-type-groups';
import {checkState, sameFilter, toggleAll, toggleGroup, toggleKind, toGridify, viewDefaultMorphTypeFilter} from './morph-type-filter';

const {Prefix: prefix, Stem: stem, Suffix: suffix, Root: root} = MorphTypeKind;
// Like the demo project: lists only a few kinds
const listed = [prefix, stem, suffix];

describe('morph type groups', () => {
  it('cover every MorphTypeKind except Unknown exactly once', () => {
    const grouped = [...WORDS_AND_PHRASES, ...AFFIXES, ...CLITICS].sort();
    const kinds = Object.values(MorphTypeKind).filter(k => k !== MorphTypeKind.Unknown).sort();
    expect(grouped).toEqual(kinds);
  });
});

describe('morph type filter', () => {
  it('defaults to words and phrases in Lite and no filter in Classic', () => {
    expect(viewDefaultMorphTypeFilter(ViewBase.FwLite)).toEqual(WORDS_AND_PHRASES);
    expect(viewDefaultMorphTypeFilter(ViewBase.FieldWorks)).toBe('all');
  });

  it('treats all and empty as the same no-op filter, ignoring order', () => {
    expect(sameFilter([], 'all')).toBe(true);
    expect(sameFilter([stem, root], [root, stem])).toBe(true);
    expect(toGridify([])).toBeUndefined();
    expect(toGridify('all')).toBeUndefined();
    expect(toGridify([stem, root])).toBe('(MorphType=Stem|MorphType=Root)');
  });

  it('toggling a fully selected group off removes its unlisted kinds too', () => {
    expect(toggleGroup([...WORDS_AND_PHRASES, prefix], WORDS_AND_PHRASES, listed)).toEqual([prefix]);
  });

  it('toggling a group on adds its full definition', () => {
    expect(sameFilter(toggleGroup([], WORDS_AND_PHRASES, listed), [...WORDS_AND_PHRASES])).toBe(true);
  });

  it('selecting every listed kind becomes all', () => {
    expect(toggleGroup([...WORDS_AND_PHRASES], AFFIXES, listed)).toBe('all');
    expect(toggleKind([prefix, stem], suffix, listed)).toBe('all');
  });

  it('toggling a kind off from all keeps the other listed kinds', () => {
    expect(toggleKind('all', stem, listed)).toEqual([prefix, suffix]);
  });

  it('toggling a kind on adds just that kind', () => {
    expect(toggleKind([], stem, listed)).toEqual([stem]);
  });

  it('toggling a partly selected group selects the whole group', () => {
    expect(checkState([prefix], AFFIXES.filter(k => listed.includes(k)))).toBe('some');
    expect(toggleGroup([prefix], AFFIXES, [prefix, suffix, root])).toEqual(AFFIXES);
  });

  it('toggling a group off from all keeps the other listed kinds', () => {
    expect(toggleGroup('all', AFFIXES, listed)).toEqual([stem]);
  });

  it('toggles all on and off', () => {
    expect(toggleAll([stem], listed)).toBe('all');
    expect(toggleAll('all', listed)).toEqual([]);
  });
});
