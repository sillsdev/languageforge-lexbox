import {makeDiff, cleanupSemantic, DIFF_DELETE, DIFF_INSERT} from '@sanity/diff-match-patch';

export type DiffSegmentType = 'equal' | 'added' | 'removed';

export interface DiffSegment {
  value: string;
  type: DiffSegmentType;
}

const graphemes = new Intl.Segmenter(undefined, {granularity: 'grapheme'});

/**
 * Diffs two strings with diff-match-patch's semantic cleanup, which rolls character-level edits up into
 * readable word-sized chunks. Concatenating the non-added segments rebuilds `before`; the non-removed ones rebuild `after`.
 */
export function computeDiff(before: string, after: string): DiffSegment[] {
  const codec = graphemeCodec(before + after);
  const diffs = cleanupSemantic(makeDiff(codec.encode(before), codec.encode(after)));
  return diffs.map(([op, value]) => ({
    value: codec.decode(value),
    type: op === DIFF_INSERT ? 'added' : op === DIFF_DELETE ? 'removed' : 'equal',
  }));
}

export function hasChanges(segments: DiffSegment[]): boolean {
  return segments.some((s) => s.type !== 'equal');
}

// diff-match-patch diffs UTF-16 units, so it would split a combining mark from its base letter. Swapping each
// multi-unit grapheme for one private-use char the text doesn't contain makes graphemes the unit of diffing.
function graphemeCodec(text: string) {
  const used = new Set(text);
  const codeOf = new Map<string, string>();
  const graphemeOf = new Map<string, string>();
  let next = 0xe000;

  function code(grapheme: string): string {
    let c = codeOf.get(grapheme);
    if (!c) {
      while (used.has(String.fromCharCode(next))) next++;
      c = String.fromCharCode(next++);
      codeOf.set(grapheme, c);
      graphemeOf.set(c, grapheme);
    }
    return c;
  }

  return {
    encode: (s: string) => Array.from(graphemes.segment(s), ({segment}) => (segment.length > 1 ? code(segment) : segment)).join(''),
    decode: (s: string) => Array.from(s, (char) => graphemeOf.get(char) ?? char).join(''),
  };
}
