import type { IEntry, IMultiString, WritingSystem, WritingSystems } from "./mini-lcm";
import type { WritingSystemSelection, WritingSystemType } from "./types";

export function firstVal(multi: IMultiString): string | undefined {
  return Object.values(multi).find(value => !!value);
}

export function firstDefVal(entry: IEntry): string | undefined {
  if (entry.senses.length == 0) return undefined;
  const multi = entry.senses[0].definition;
  return Object.values(multi).find(value => !!value);
}

export function pickWritingSystems(
  ws: WritingSystemSelection,
  allWs: WritingSystems,
): WritingSystem[] {
  switch (ws) {
    case "vernacular-analysis":
      return [...new Set([...allWs.vernacular, ...allWs.analysis].sort())];
    case "analysis-vernacular":
      return [...new Set([...allWs.analysis, ...allWs.vernacular].sort())];
    case "first-analysis":
      return [allWs.analysis[0]];
    case "first-vernacular":
      return [allWs.vernacular[0]];
    case "vernacular":
      return allWs.vernacular;
    case "analysis":
      return allWs.analysis;
  }
}
