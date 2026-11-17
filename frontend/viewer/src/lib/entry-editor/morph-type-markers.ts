import {MorphTypeKind, type IMorphType} from '$lib/dotnet-types';

type MarkerSource = Pick<IMorphType, 'kind' | 'prefix' | 'postfix'>;

export type MorphTypeMatch =
  | {valid: true, kind: MorphTypeKind, leading: string, trailing: string}
  | {valid: false, leading: string, trailing: string};

// FLEx can't tell these apart by markers, so it picks the common type (liblcm MorphServices.FindMorphType)
const ambiguousToDefault: Partial<Record<MorphTypeKind, MorphTypeKind>> = {
  [MorphTypeKind.PrefixingInterfix]: MorphTypeKind.Prefix,
  [MorphTypeKind.SuffixingInterfix]: MorphTypeKind.Suffix,
  [MorphTypeKind.InfixingInterfix]: MorphTypeKind.Infix,
  [MorphTypeKind.BoundRoot]: MorphTypeKind.BoundStem,
  [MorphTypeKind.Particle]: MorphTypeKind.Stem,
  [MorphTypeKind.Circumfix]: MorphTypeKind.Stem,
  [MorphTypeKind.Root]: MorphTypeKind.Stem,
  [MorphTypeKind.DiscontiguousPhrase]: MorphTypeKind.Stem,
  [MorphTypeKind.Phrase]: MorphTypeKind.Stem,
  [MorphTypeKind.Clitic]: MorphTypeKind.Stem,
};

function longest(markers: string[]): string {
  return markers.reduce((a, b) => b.length > a.length ? b : a, '');
}

/** Mirrors FLEx's InsertEntryDlg: detects the morph type from affix markers like `-ed`, `re-` or `*bound`. */
export function findMorphType(form: string, morphTypes: MarkerSource[]): MorphTypeMatch {
  const trimmed = form.trim();
  const leading = longest(morphTypes.map(mt => mt.prefix ?? '').filter(p => p && trimmed.startsWith(p)));
  const trailing = longest(morphTypes.map(mt => mt.postfix ?? '')
    .filter(p => p && trimmed.length > p.length && trimmed.endsWith(p)));
  if (trimmed.length - leading.length - trailing.length <= 0) {
    // a lone marker (e.g. the user has only typed "-" so far) isn't an affix yet
    return {valid: true, kind: MorphTypeKind.Stem, leading: '', trailing: ''};
  }
  const match = morphTypes.find(mt => (mt.prefix ?? '') === leading && (mt.postfix ?? '') === trailing);
  if (!match && (leading || trailing)) return {valid: false, leading, trailing};
  let kind = match ? ambiguousToDefault[match.kind] ?? match.kind : MorphTypeKind.Stem;
  if (kind === MorphTypeKind.Stem && trimmed.includes(' ')) kind = MorphTypeKind.Phrase;
  return {valid: true, kind, leading, trailing};
}

export function stripMarkers(form: string, match: Pick<MorphTypeMatch, 'leading' | 'trailing'>): string {
  let result = form.trim();
  if (match.leading && result.startsWith(match.leading)) result = result.slice(match.leading.length);
  if (match.trailing && result.endsWith(match.trailing)) result = result.slice(0, result.length - match.trailing.length);
  return result;
}
