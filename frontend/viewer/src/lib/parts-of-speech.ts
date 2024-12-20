import {derived, type Readable, type Writable, writable} from 'svelte/store';
import type {IPartOfSpeech, IWritingSystems} from '$lib/dotnet-types';
import {useLexboxApi} from './services/service-provider';
import {pickBestAlternative} from './utils';

type LabeledPartOfSpeech = IPartOfSpeech & {label: string};

let partsOfSpeechStore: Writable<IPartOfSpeech[] | null> | null = null;

export function usePartsOfSpeech(writingSystemsStore: Readable<IWritingSystems>): Readable<LabeledPartOfSpeech[]> {
  if (partsOfSpeechStore === null) {
    partsOfSpeechStore = writable<IPartOfSpeech[] | null>([], (set) => {
      useLexboxApi().getPartsOfSpeech().then(partsOfSpeech => {
        set(partsOfSpeech);
      }).catch(error => {
        console.error('Failed to load parts of speech', error);
        throw error;
      });
    });
  }
  return derived([partsOfSpeechStore, writingSystemsStore], ([partsOfSpeech, writingSystems]) => {
    return (partsOfSpeech ?? []).map(partOfSpeech => ({
      ...partOfSpeech,
      label: pickBestAlternative(partOfSpeech.name, 'analysis', writingSystems),
    }));
  });
}
