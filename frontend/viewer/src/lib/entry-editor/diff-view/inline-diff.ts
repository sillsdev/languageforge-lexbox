import {makeDiff, cleanupSemantic, DIFF_DELETE, DIFF_INSERT} from '@sanity/diff-match-patch';

export type DiffSegmentType = 'equal' | 'added' | 'removed';

export interface DiffSegment {
  value: string;
  type: DiffSegmentType;
}

/**
 * Diffs two strings with diff-match-patch's semantic cleanup, which rolls character-level edits up into
 * readable word-sized chunks. Concatenating the non-added segments rebuilds `before`; the non-removed ones rebuild `after`.
 */
export function computeDiff(before: string, after: string): DiffSegment[] {
  const diffs = cleanupSemantic(makeDiff(before, after));
  return diffs.map(([op, value]) => ({
    value,
    type: op === DIFF_INSERT ? 'added' : op === DIFF_DELETE ? 'removed' : 'equal',
  }));
}

export function hasChanges(segments: DiffSegment[]): boolean {
  return segments.some((s) => s.type !== 'equal');
}
