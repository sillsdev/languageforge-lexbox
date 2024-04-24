import type { IEntry, IMultiString, ISense, WritingSystem, WritingSystems } from './mini-lcm';

import type { WritingSystemSelection } from './config-types';

export function firstVal(multi: IMultiString): string | undefined {
  return Object.values(multi).find(value => !!value);
}

export function firstDefOrGlossVal(sense: ISense | undefined): string {
  if (!sense) return '';
  const definition = Object.values(sense.definition ?? {}).find(value => !!value);
  if (definition) return definition;
  return Object.values(sense.gloss ?? {}).find(value => !!value) ?? ''
}

export function pickWritingSystems(
  ws: WritingSystemSelection,
  allWs: WritingSystems,
): WritingSystem[] {
  switch (ws) {
    case 'vernacular-analysis':
      return [...new Set([...allWs.vernacular, ...allWs.analysis].sort())];
    case 'analysis-vernacular':
      return [...new Set([...allWs.analysis, ...allWs.vernacular].sort())];
    case 'first-analysis':
      return [allWs.analysis[0]];
    case 'first-vernacular':
      return [allWs.vernacular[0]];
    case 'vernacular':
      return allWs.vernacular;
    case 'analysis':
      return allWs.analysis;
  }
}
