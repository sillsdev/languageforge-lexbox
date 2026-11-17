import {describe, expect, it} from 'vitest';
import {MorphTypeKind as K} from '$lib/dotnet-types';
import {findMorphType, stripMarkers} from './morph-type-markers';

// FLEx's canonical markers (MiniLcm CanonicalMorphTypes)
const morphTypes = [
  {kind: K.BoundRoot, prefix: '*'},
  {kind: K.BoundStem, prefix: '*'},
  {kind: K.Circumfix},
  {kind: K.Clitic},
  {kind: K.Enclitic, prefix: '='},
  {kind: K.Infix, prefix: '-', postfix: '-'},
  {kind: K.Particle},
  {kind: K.Prefix, postfix: '-'},
  {kind: K.Proclitic, postfix: '='},
  {kind: K.Root},
  {kind: K.Simulfix, prefix: '=', postfix: '='},
  {kind: K.Stem},
  {kind: K.Suffix, prefix: '-'},
  {kind: K.Suprafix, prefix: '~', postfix: '~'},
  {kind: K.InfixingInterfix, prefix: '-', postfix: '-'},
  {kind: K.PrefixingInterfix, postfix: '-'},
  {kind: K.SuffixingInterfix, prefix: '-'},
  {kind: K.Phrase},
  {kind: K.DiscontiguousPhrase},
];

describe('findMorphType', () => {
  it.each([
    ['walk', K.Stem, 'walk'],
    ['  walk ', K.Stem, 'walk'],
    ['kick the bucket', K.Phrase, 'kick the bucket'],
    ['-ed', K.Suffix, 'ed'],
    ['re-', K.Prefix, 're'],
    ['-a-', K.Infix, 'a'],
    ['=lo', K.Enclitic, 'lo'],
    ['lo=', K.Proclitic, 'lo'],
    ['=a=', K.Simulfix, 'a'],
    ['~a~', K.Suprafix, 'a'],
    ['*bound', K.BoundStem, 'bound'],
    ['-', K.Stem, '-'],
    ['', K.Stem, ''],
  ])('%j is %s, stored as %j', (form, kind, stored) => {
    const match = findMorphType(form, morphTypes);
    expect(match).toMatchObject({valid: true, kind});
    expect(stripMarkers(form, match)).toBe(stored);
  });

  it.each(['*foo-', '~foo', 'foo~', '=foo-'])('rejects marker combination %j', form => {
    expect(findMorphType(form, morphTypes).valid).toBe(false);
  });

  it('treats unmarked forms as stems before the morph types have loaded', () => {
    expect(findMorphType('walk', [])).toMatchObject({valid: true, kind: K.Stem});
  });

  it('uses the project\'s own markers', () => {
    const custom = [{kind: K.Stem}, {kind: K.Suffix, prefix: '--'}];
    const match = findMorphType('--ed', custom);
    expect(match).toMatchObject({valid: true, kind: K.Suffix});
    expect(stripMarkers('--ed', match)).toBe('ed');
  });
});
