import {describe, expect, it} from 'vitest';
import {MorphTypeKind as K} from '$lib/dotnet-types';
import {checkLexemeForm, findMorphType, fitsMorphType, stripMarkers} from './morph-type-markers';

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

describe('morph type markers', () => {
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
    ['--s', K.Suffix, '-s'],
    ['-', K.Suffix, ''],
    ['*', K.BoundStem, ''],
    ['~', K.Suprafix, ''],
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

  it.each([
    [K.Root, K.Stem, true],
    [K.Particle, K.Phrase, true],
    [K.BoundRoot, K.BoundStem, true],
    [K.SuffixingInterfix, K.Suffix, true],
    [K.Suffix, K.Stem, false],
    [K.Root, K.BoundStem, false],
  ])('%s fits a form detected as %s: %s', (chosen, detected, fits) => {
    expect(fitsMorphType(chosen, detected)).toBe(fits);
  });

  it.each([
    ['walk', K.Stem, {stored: 'walk'}],
    ['walk', K.Root, {stored: 'walk'}],
    ['-ed', K.Suffix, {stored: 'ed'}],
    ['--ed', K.Suffix, {stored: '-ed', problem: {kind: 'leftoverMarker', marker: '-'}}],
    ['*ed-', K.Suffix, {stored: 'ed', problem: {kind: 'unmatchedMarkers', leading: '*', trailing: '-'}}],
    ['ed', K.Suffix, {stored: 'ed', problem: {kind: 'mismatch'}}],
    ['a- -b', K.Circumfix, {stored: 'a- -b'}],
    ['a-.-b', K.Circumfix, {stored: 'a-.-b'}],
    ['ab', K.Circumfix, {stored: 'ab', problem: {kind: 'incompleteCircumfix'}}],
    [' -b', K.Circumfix, {stored: 'b', problem: {kind: 'mismatch'}}],
    ['a- ~b', K.Circumfix, {stored: 'a- ~b', problem: {kind: 'incompleteCircumfix'}}],
  ])('%j as %s is checked as %j', (form, chosen, check) => {
    expect(checkLexemeForm(form, chosen, morphTypes)).toEqual(check);
  });

  it('keeps an interfix that has its own markers', () => {
    const custom = [{kind: K.Stem}, {kind: K.Prefix, postfix: '-'}, {kind: K.PrefixingInterfix, postfix: '+'}];
    expect(findMorphType('ed+', custom)).toMatchObject({valid: true, kind: K.PrefixingInterfix});
    expect(findMorphType('ed-', custom)).toMatchObject({valid: true, kind: K.Prefix});
  });

  it('rejects the markers of morph types FW Lite does not know', () => {
    const custom = [{kind: K.Stem}, {kind: K.Unknown, prefix: '%'}];
    expect(findMorphType('%ed', custom)).toMatchObject({valid: false, unsupported: true});
    expect(checkLexemeForm('%ed', K.Stem, custom).problem).toEqual({kind: 'unsupportedMorphType'});
    expect(findMorphType('%', custom)).toMatchObject({valid: false, unsupported: true});
  });

  it('uses the project\'s own markers', () => {
    const custom = [{kind: K.Stem}, {kind: K.Suffix, prefix: '--'}];
    const match = findMorphType('--ed', custom);
    expect(match).toMatchObject({valid: true, kind: K.Suffix});
    expect(stripMarkers('--ed', match)).toBe('ed');
  });
});
