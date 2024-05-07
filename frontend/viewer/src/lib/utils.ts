import type { IEntry, IExampleSentence, IMultiString, ISense, WritingSystem, WritingSystems } from './mini-lcm';

import type { WritingSystemSelection } from './config-types';

export function firstVal(multi: IMultiString): string | undefined {
  return Object.values(multi).find(value => !!value);
}

export function headword(entry: IEntry): string {
  return firstVal(entry.citationForm) ?? firstVal(entry.lexemeForm) ?? '';
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

export function filterEntries(entries: IEntry[], query: string) {
  return entries.filter(entry =>
    [
      ...Object.values(entry.lexemeForm ?? {}),
      ...Object.values(entry.citationForm ?? {}),
      ...Object.values(entry.literalMeaning ?? {}),
    ].some(value => value?.toLowerCase().includes(query.toLowerCase())))
}

export function defaultEntry(): IEntry {
  return {
    id: crypto.randomUUID(),
    citationForm: {},
    lexemeForm: {},
    note: {},
    literalMeaning: {},
    senses: []
  };
}

export function defaultSense(): ISense {
  return {
    id: crypto.randomUUID(),
    definition: {},
    gloss: {},
    partOfSpeech: '',
    semanticDomain: [],
    exampleSentences: []
  };
}

export function defaultExampleSentence(): IExampleSentence {
  return {
    id: crypto.randomUUID(),
    sentence: {},
    translation: {},
    reference: '',
  };
}
