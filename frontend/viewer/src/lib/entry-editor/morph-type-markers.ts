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

function normalize(kind: MorphTypeKind): MorphTypeKind {
  return ambiguousToDefault[kind] ?? kind;
}

function longest(markers: string[]): string {
  return markers.reduce((a, b) => b.length > a.length ? b : a, '');
}

// FLEx's GetTypeIfMatchesPrefix: typing just "-" already means suffix
function loneMarkerType(form: string, morphTypes: MarkerSource[]): MorphTypeKind | undefined {
  const matches = morphTypes.filter(mt => mt.prefix === form);
  const match = matches.find(mt => !mt.postfix) ?? matches[0];
  return match && normalize(match.kind);
}

/** Mirrors FLEx's InsertEntryDlg: detects the morph type from affix markers like `-ed`, `re-` or `*bound`. */
export function findMorphType(form: string, morphTypes: MarkerSource[]): MorphTypeMatch {
  const trimmed = form.trim();
  const loneKind = loneMarkerType(trimmed, morphTypes);
  if (loneKind) return {valid: true, kind: loneKind, leading: trimmed, trailing: ''};

  const leading = longest(morphTypes.map(mt => mt.prefix ?? '').filter(p => p && trimmed.startsWith(p)));
  const trailing = longest(morphTypes.map(mt => mt.postfix ?? '')
    .filter(p => p && trimmed.length > p.length && trimmed.endsWith(p)));
  const match = morphTypes.find(mt => (mt.prefix ?? '') === leading && (mt.postfix ?? '') === trailing);
  if (!match && (leading || trailing)) return {valid: false, leading, trailing};
  let kind = match ? normalize(match.kind) : MorphTypeKind.Stem;
  if (kind === MorphTypeKind.Stem && trimmed.includes(' ')) kind = MorphTypeKind.Phrase;
  return {valid: true, kind, leading, trailing};
}

export function stripMarkers(form: string, match: Pick<MorphTypeMatch, 'leading' | 'trailing'>): string {
  let result = form.trim();
  if (match.leading && result.startsWith(match.leading)) result = result.slice(match.leading.length);
  if (match.trailing && result.endsWith(match.trailing)) result = result.slice(0, result.length - match.trailing.length);
  return result;
}

/** FLEx's CheckMorphType: e.g. a form detected as a stem is fine for a root, particle or phrase */
export function fitsMorphType(chosen: MorphTypeKind, detected: MorphTypeKind): boolean {
  return normalize(chosen) === normalize(detected);
}

/** True for a stored form like "-s" (from "--s"), which would still read as marked */
export function hasLeftoverMarkers(stored: string, morphTypes: MarkerSource[]): boolean {
  if (!stored.trim()) return false;
  const match = findMorphType(stored, morphTypes);
  return !match.valid || !!match.leading || !!match.trailing;
}
