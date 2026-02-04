import {MorphType, type IEntry, type IExampleSentence, type ISense, type ITranslation, type IWritingSystem, type WritingSystemType} from '$lib/dotnet-types';
import {get, writable, type Readable} from 'svelte/store';
import {type ClassValue, clsx} from 'clsx';
import {twMerge} from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type WithoutChild<T> = T extends { child?: any } ? Omit<T, 'child'> : T;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type WithoutChildren<T> = T extends { children?: any } ? Omit<T, 'children'> : T;
export type WithoutChildrenOrChild<T> = WithoutChildren<WithoutChild<T>>;
export type WithElementRef<T, U extends HTMLElement = HTMLElement> = T & { ref?: U | null };

export function randomId(): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
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
    id: randomId(),
    citationForm: {},
    lexemeForm: {},
    note: {},
    literalMeaning: {},
    senses: [],
    complexForms: [],
    complexFormTypes: [],
    components: [],
    publishIn: [],
    morphType: MorphType.Stem,
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
    translations: [],
    reference: {spans: []},
  };
}

export function defaultTranslation(): ITranslation {
  return {
    id: randomId(),
    text: {},
  };
}

type Draft = {saveDraft: () => void};

export function draftTranslation(exampleSentence: IExampleSentence): ITranslation & Draft {
  const draftTranslation = defaultTranslation() as ITranslation & Draft;
  draftTranslation.saveDraft = () => {
    if (!exampleSentence.translations) {
      exampleSentence.translations = [];
    }
    const {saveDraft, ...translation} = draftTranslation;
    exampleSentence.translations.push(translation);
  };
  return draftTranslation;
}

export function isDraft(entity: object): entity is Draft {
  return 'saveDraft' in entity && typeof entity.saveDraft === 'function';
}

export function defaultWritingSystem(type: WritingSystemType): IWritingSystem {
  return {
    id: randomId(),
    wsId: 'en',
    isAudio: false,
    name: 'English',
    abbreviation: 'en',
    font: 'Arial',
    exemplars: [],
    deletedAt: undefined,
    type
  };
}

export function isEntry(data: IEntry | ISense | IExampleSentence | undefined): data is IEntry {
  return !!data && !isSense(data) && !isExample(data) && 'senses' in data && 'lexemeForm' in data;
}

export function isSense(data: IEntry | ISense | IExampleSentence | undefined): data is ISense {
  return !!data && 'entryId' in data;
}

export function isExample(data: IEntry | ISense | IExampleSentence | undefined): data is IExampleSentence {
  return !!data && 'senseId' in data;
}
export function makeHasHadValueTracker(): {store: Readable<boolean>, pushAndGet(currValueOrHasValue?: unknown): boolean} {
  const hasHadValueStore = writable<boolean>();
  function pushAndGet(currValueOrHasValue?: unknown) {
    hasHadValueStore.update(hasHadValue => {
      if (hasHadValue) return true;
      return !!currValueOrHasValue;
    });
    return get(hasHadValueStore);
  }

  return {store: hasHadValueStore, pushAndGet};
}
