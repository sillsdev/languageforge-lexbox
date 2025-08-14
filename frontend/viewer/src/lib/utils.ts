import type {IEntry, IExampleSentence, ISense, IWritingSystem, WritingSystemType} from '$lib/dotnet-types';
import {get, writable, type Readable} from 'svelte/store';
import {type ClassValue, clsx} from 'clsx';
import {twMerge} from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

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
    reference: {spans: []},
  };
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
