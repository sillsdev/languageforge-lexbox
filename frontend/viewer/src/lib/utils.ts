import type {IEntry, IExampleSentence, ISense, IWritingSystem, WritingSystemType} from '$lib/dotnet-types';

export function randomId(): string {
  return crypto.randomUUID();
}

export function firstTruthy<T, U>(items: T[], mapper: (item: T) => U): U | undefined {
  for (const item of items) {
    const mapped = mapper(item);
    if (mapped) return mapped;
  }
  return undefined;
}

export function defaultEntry(): IEntry {
  return {
    id: crypto.randomUUID(),
    citationForm: {},
    lexemeForm: {},
    note: {},
    literalMeaning: {},
    senses: [],
    complexForms: [],
    complexFormTypes: [],
    components: [],
  };
}

export function defaultSense(entryId: string): ISense {
  return {
    id: randomId(),
    entryId,
    definition: {},
    gloss: {},
    partOfSpeechId: undefined,
    partOfSpeech: undefined,
    semanticDomains: [],
    exampleSentences: []
  };
}

export function defaultExampleSentence(senseId: string): IExampleSentence {
  return {
    id: randomId(),
    senseId,
    sentence: {},
    translation: {},
    reference: '',
  };
}

export function defaultWritingSystem(type: WritingSystemType): IWritingSystem {
  return {
    id: randomId(),
    wsId: 'en',
    name: 'English',
    abbreviation: 'en',
    font: 'Arial',
    exemplars: [],
    order: 0,
    deletedAt: undefined,
    type
  };
}
