/** The ids in both lists whose place among the shared ones changed, which is all a reorder shows as. */
export function movedIds(before: readonly string[], after: readonly string[]): Set<string> {
  const beforeIds = new Set(before);
  const afterIds = new Set(after);
  const keptBefore = before.filter((id) => afterIds.has(id));
  const keptAfter = after.filter((id) => beforeIds.has(id));
  return new Set(keptAfter.filter((id, i) => keptBefore[i] !== id));
}
