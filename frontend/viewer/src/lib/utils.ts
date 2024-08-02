import type { IEntry, IExampleSentence, IMultiString, ISense, WritingSystem, WritingSystems } from './mini-lcm';

import type { WritingSystemSelection } from './config-types';

export function firstVal(multi: IMultiString): string | undefined {
  return Object.values(multi).find(value => !!value);
}

export function headword(entry: IEntry, ws?: string): string {
  if (ws) {
    return entry.citationForm[ws] ?? entry.lexemeForm[ws] ?? '';
  } else {
    return firstVal(entry.citationForm) ?? firstVal(entry.lexemeForm) ?? '';
  }
}

export function pickBestAlternative(value: IMultiString, firstChoice?: WritingSystem): string {
  let pick = firstChoice ? value[firstChoice.id] : undefined;
  return pick ?? firstVal(value) ?? '';
}

export function firstDefOrGlossVal(sense: ISense | undefined): string {
  if (!sense) return '';
  const definition = Object.values(sense.definition ?? {}).find(value => !!value);
  if (definition) return definition;
  return Object.values(sense.gloss ?? {}).find(value => !!value) ?? ''
}

export function firstSentenceOrTranslationVal(example: IExampleSentence | undefined): string {
  if (!example) return '';
  const sentence = Object.values(example.sentence ?? {}).find(value => !!value);
  if (sentence) return sentence;
  return Object.values(example.translation ?? {}).find(value => !!value) ?? ''
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

const emptyIdPrefix = '00000000-0000-0000-0000-';
export function emptyId(): string {
  return emptyIdPrefix + crypto.randomUUID().slice(emptyIdPrefix.length);
}
export function isEmptyId(id: string): boolean {
  return id.startsWith(emptyIdPrefix);
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
    id: emptyId(),
    definition: {},
    gloss: {},
    partOfSpeechId: undefined,
    semanticDomains: [],
    exampleSentences: []
  };
}

export function defaultExampleSentence(): IExampleSentence {
  return {
    id: emptyId(),
    sentence: {},
    translation: {},
    reference: '',
  };
}
