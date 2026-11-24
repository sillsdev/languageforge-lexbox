import {MorphTypeKind, type IMorphType} from '$lib/dotnet-types';

type MarkerSource = Pick<IMorphType, 'kind' | 'prefix' | 'postfix'>;

export type MorphTypeMatch =
  | {valid: true, kind: MorphTypeKind, leading: string, trailing: string}
  | {valid: false, leading: string, trailing: string, unsupported?: boolean};

// FLEx can't tell these apart by markers, so it picks the common type (liblcm MorphServices.FindMorphType)
const ambiguousToDefault: Partial<Record<MorphTypeKind, MorphTypeKind>> = {
  [MorphTypeKind.PrefixingInterfix]: MorphTypeKind.Prefix,
  [MorphTypeKind.SuffixingInterfix]: MorphTypeKind.Suffix,
  [MorphTypeKind.InfixingInterfix]: MorphTypeKind.Infix,
  [MorphTypeKind.BoundRoot]: MorphTypeKind.BoundStem,
  [MorphTypeKind.Particle]: MorphTypeKind.Stem,
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

function sameMarkers(a: MarkerSource, b: MarkerSource): boolean {
  return (a.prefix ?? '') === (b.prefix ?? '') && (a.postfix ?? '') === (b.postfix ?? '');
}

// FLEx never detects these from markers, so unmarked they read as a stem
const stemLike = new Set([MorphTypeKind.Particle, MorphTypeKind.Circumfix, MorphTypeKind.Root,
  MorphTypeKind.DiscontiguousPhrase, MorphTypeKind.Phrase, MorphTypeKind.Clitic]);

// Interfixes and bound roots read as their common type, like in FLEx, unless the project gave them their own markers
function detectedKind(match: MarkerSource, morphTypes: MarkerSource[]): MorphTypeKind {
  if (stemLike.has(match.kind) && !match.prefix && !match.postfix) return MorphTypeKind.Stem;
  const common = morphTypes.find(mt => mt.kind === ambiguousToDefault[match.kind]);
  return common && sameMarkers(common, match) ? common.kind : match.kind;
}

// FLEx's GetTypeIfMatchesPrefix: typing just "-" already means suffix
function loneMarkerType(form: string, morphTypes: MarkerSource[]): MorphTypeKind | undefined {
  const matches = morphTypes.filter(mt => mt.prefix === form && mt.kind !== MorphTypeKind.Unknown);
  const match = matches.find(mt => !mt.postfix) ?? matches[0];
  return match && detectedKind(match, morphTypes);
}

/** Mirrors FLEx's InsertEntryDlg: detects the morph type from affix markers like `-ed`, `re-` or `*bound`. */
export function findMorphType(form: string, morphTypes: MarkerSource[]): MorphTypeMatch {
  const trimmed = form.trim();
  const loneKind = loneMarkerType(trimmed, morphTypes);
  if (loneKind) return {valid: true, kind: loneKind, leading: trimmed, trailing: ''};

  const leading = longest(morphTypes.map(mt => mt.prefix ?? '').filter(p => p && trimmed.startsWith(p)));
  const trailing = longest(morphTypes.map(mt => mt.postfix ?? '')
    .filter(p => p && trimmed.length > p.length && trimmed.endsWith(p)));
  const matches = morphTypes.filter(mt => (mt.prefix ?? '') === leading && (mt.postfix ?? '') === trailing);
  const match = matches.find(mt => mt.kind !== MorphTypeKind.Unknown) ?? matches[0];
  if (!match && (leading || trailing)) return {valid: false, leading, trailing};
  // A project's own morph types have no kind FW Lite can create entries with
  if (match?.kind === MorphTypeKind.Unknown) return {valid: false, leading, trailing, unsupported: true};
  let kind = match ? detectedKind(match, morphTypes) : MorphTypeKind.Stem;
  if (kind === MorphTypeKind.Stem && looksLikeCircumfix(trimmed, morphTypes)) kind = MorphTypeKind.Circumfix;
  else if (kind === MorphTypeKind.Stem && trimmed.includes(' ')) kind = MorphTypeKind.Phrase;
  return {valid: true, kind, leading, trailing};
}

// liblcm StringServices.GetCircumfixLeftAndRightParts: "a- -b" or "a-.-b"
function circumfixParts(form: string): [left: string, right: string] | undefined {
  const leftEnd = form.search(/[ .]/);
  if (leftEnd < 0) return undefined;
  return [form.slice(0, leftEnd), form.slice(Math.max(form.lastIndexOf(' '), form.lastIndexOf('.')) + 1)];
}

// Not in FLEx, which reads "a- -b" as a phrase
function looksLikeCircumfix(form: string, morphTypes: MarkerSource[]): boolean {
  const parts = circumfixParts(form);
  if (!parts || !morphTypes.some(mt => mt.kind === MorphTypeKind.Circumfix)) return false;
  const [left, right] = parts.map(part => findMorphType(part, morphTypes));
  return left.valid && !!left.trailing && right.valid && !!right.leading;
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

export type LexemeFormProblem =
  | {kind: 'unmatchedMarkers', suggestions: string[]}
  | {kind: 'unsupportedMorphType'}
  | {kind: 'incompleteCircumfix'}
  | {kind: 'mismatch'}
  | {kind: 'leftoverMarker', marker: string};

export type LexemeFormCheck = {stored: string, problem?: LexemeFormProblem};

/**
 * How a typed lexeme form will be stored as the chosen morph type, and why it can't or shouldn't be.
 * Stricter than FLEx's Insert Entry dialog, which lets the chosen type disagree with the markers.
 * Only leftoverMarker is a warning; the rest block creating the entry.
 */
export function checkLexemeForm(form: string, chosen: MorphTypeKind, morphTypes: MarkerSource[]): LexemeFormCheck {
  const match = findMorphType(form, morphTypes);
  const stored = stripMarkers(form, match);
  if (!match.valid) {
    return {stored, problem: match.unsupported ? {kind: 'unsupportedMorphType'}
      : {kind: 'unmatchedMarkers', suggestions: markerSuggestions(stored, match, morphTypes)}};
  }
  if (chosen === MorphTypeKind.Circumfix) {
    if (!isCompleteCircumfix(stored, morphTypes)) return {stored, problem: {kind: 'incompleteCircumfix'}};
  } else if (!fitsMorphType(chosen, match.kind)) {
    return {stored, problem: {kind: 'mismatch'}};
  }
  const leftover = stored.trim() ? findMorphType(stored, morphTypes) : undefined;
  const marker = leftover && (leftover.leading || leftover.trailing);
  if (marker) return {stored, problem: {kind: 'leftoverMarker', marker}};
  return {stored};
}

function isCompleteCircumfix(form: string, morphTypes: MarkerSource[]): boolean {
  return circumfixParts(form)?.every(part => part.trim() && findMorphType(part, morphTypes).valid) ?? false;
}

// Forms that keep one of the typed markers, e.g. "~a~" for "a~"
function markerSuggestions(bare: string, {leading, trailing}: Pick<MorphTypeMatch, 'leading' | 'trailing'>, morphTypes: MarkerSource[]): string[] {
  const forms = morphTypes
    .filter(mt => mt.kind !== MorphTypeKind.Unknown && ((leading && mt.prefix === leading) || (trailing && mt.postfix === trailing)))
    .map(mt => `${mt.prefix ?? ''}${bare}${mt.postfix ?? ''}`);
  return [...new Set(forms)].sort((a, b) => a.length - b.length || a.localeCompare(b)).slice(0, 3);
}
