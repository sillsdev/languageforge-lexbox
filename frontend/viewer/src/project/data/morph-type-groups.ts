import {MorphTypeKind} from '$lib/dotnet-types';

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
