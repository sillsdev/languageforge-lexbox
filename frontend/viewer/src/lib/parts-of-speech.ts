import {derived, type Readable, type Writable, writable} from 'svelte/store';
import type {PartOfSpeech, WritingSystems} from './mini-lcm';
import {useLexboxApi} from './services/service-provider';
import {pickBestAlternative} from './utils';

type LabeledPartOfSpeech = PartOfSpeech & {label: string};

let partsOfSpeechStore: Writable<PartOfSpeech[] | null> | null = null;

export function usePartsOfSpeech(writingSystemsStore: Readable<WritingSystems>): Readable<LabeledPartOfSpeech[]> {
  if (partsOfSpeechStore === null) {
    partsOfSpeechStore = writable<PartOfSpeech[] | null>([], (set) => {
      useLexboxApi().GetPartsOfSpeech().then(partsOfSpeech => {
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
