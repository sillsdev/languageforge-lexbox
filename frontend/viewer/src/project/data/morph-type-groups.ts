import {MorphTypeKind} from '$lib/dotnet-types';
import {msg} from 'svelte-i18n-lingui';

export const WORDS_AND_PHRASES: readonly MorphTypeKind[] = [
  MorphTypeKind.Root, MorphTypeKind.BoundRoot,
  MorphTypeKind.Stem, MorphTypeKind.BoundStem,
  MorphTypeKind.Particle,
  MorphTypeKind.Phrase, MorphTypeKind.DiscontiguousPhrase,
];

// Same set as LcmHelpers.IsAffixMorphType in FwDataMiniLcmBridge
export const AFFIXES: readonly MorphTypeKind[] = [
  MorphTypeKind.Prefix, MorphTypeKind.Suffix, MorphTypeKind.Infix,
  MorphTypeKind.Circumfix, MorphTypeKind.Simulfix, MorphTypeKind.Suprafix,
  MorphTypeKind.PrefixingInterfix, MorphTypeKind.InfixingInterfix, MorphTypeKind.SuffixingInterfix,
];

export const CLITICS: readonly MorphTypeKind[] = [
  MorphTypeKind.Clitic, MorphTypeKind.Enclitic, MorphTypeKind.Proclitic,
];

export const MORPH_TYPE_GROUPS = [
  {key: '__words__', label: msg`Words and phrases`, kinds: WORDS_AND_PHRASES},
  {key: '__affixes__', label: msg`Affixes`, kinds: AFFIXES},
  {key: '__clitics__', label: msg`Clitics`, kinds: CLITICS},
];
export const OTHER_MORPH_TYPES = {key: '__other__', label: msg`Other`, kinds: [] as readonly MorphTypeKind[]};

/** Sorts like the groups, with Other last */
export function morphTypeGroupIndex(kind: MorphTypeKind): number {
  const index = MORPH_TYPE_GROUPS.findIndex(g => g.kinds.includes(kind));
  return index < 0 ? MORPH_TYPE_GROUPS.length : index;
}

export function morphTypeGroup(kind: MorphTypeKind): typeof OTHER_MORPH_TYPES {
  return MORPH_TYPE_GROUPS[morphTypeGroupIndex(kind)] ?? OTHER_MORPH_TYPES;
}
