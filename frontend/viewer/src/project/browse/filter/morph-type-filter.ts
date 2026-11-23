import {ViewBase, type MorphTypeKind} from '$lib/dotnet-types';
import {WORDS_AND_PHRASES} from '$project/data/morph-type-groups';

/** 'all' and [] both mean no filter. May hold kinds the project doesn't list (e.g. the Lite default). */
export type MorphTypeFilter = MorphTypeKind[] | 'all';
export type CheckState = 'all' | 'some' | 'none';

export function viewDefaultMorphTypeFilter(base: ViewBase): MorphTypeFilter {
  return base === ViewBase.FwLite ? [...WORDS_AND_PHRASES] : 'all';
}

export function isNoFilter(filter: MorphTypeFilter): boolean {
  return filter === 'all' || filter.length === 0;
}

function filterKey(filter: MorphTypeFilter): string {
  return filter === 'all' ? '' : [...filter].sort().join();
}

export function sameFilter(a: MorphTypeFilter, b: MorphTypeFilter): boolean {
  return filterKey(a) === filterKey(b);
}

export function selectedKinds(filter: MorphTypeFilter, listed: readonly MorphTypeKind[]): MorphTypeKind[] {
  return filter === 'all' ? [...listed] : filter;
}

export function checkState(selected: readonly MorphTypeKind[], kinds: readonly MorphTypeKind[]): CheckState {
  const count = kinds.filter(k => selected.includes(k)).length;
  return count === 0 ? 'none' : count === kinds.length ? 'all' : 'some';
}

function normalize(kinds: MorphTypeKind[], listed: readonly MorphTypeKind[]): MorphTypeFilter {
  return listed.length > 0 && listed.every(k => kinds.includes(k)) ? 'all' : kinds;
}

export function toggleAll(filter: MorphTypeFilter, listed: readonly MorphTypeKind[]): MorphTypeFilter {
  return checkState(selectedKinds(filter, listed), listed) === 'all' ? [] : 'all';
}

/** groupKinds: the group's full definition, so toggling also covers kinds the project doesn't list. */
export function toggleGroup(filter: MorphTypeFilter, groupKinds: readonly MorphTypeKind[], listed: readonly MorphTypeKind[]): MorphTypeFilter {
  const selected = selectedKinds(filter, listed);
  const without = selected.filter(k => !groupKinds.includes(k));
  const listedInGroup = listed.filter(k => groupKinds.includes(k));
  return checkState(selected, listedInGroup) === 'all' ? without : normalize([...without, ...groupKinds], listed);
}

export function toggleKind(filter: MorphTypeFilter, kind: MorphTypeKind, listed: readonly MorphTypeKind[]): MorphTypeFilter {
  const selected = selectedKinds(filter, listed);
  return selected.includes(kind) ? selected.filter(k => k !== kind) : normalize([...selected, kind], listed);
}

export function toGridify(filter: MorphTypeFilter): string | undefined {
  if (filter === 'all' || filter.length === 0) return undefined;
  return '(' + filter.map(mt => `MorphType=${mt}`).join('|') + ')';
}
