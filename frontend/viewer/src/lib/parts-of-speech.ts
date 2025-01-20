import {derived, type Readable, type Writable, writable} from 'svelte/store';
import type {IPartOfSpeech} from '$lib/dotnet-types';
import {useLexboxApi} from './services/service-provider';
import type {WritingSystemService} from './writing-system-service';

type LabeledPartOfSpeech = IPartOfSpeech & {label: string};

let partsOfSpeechStore: Writable<IPartOfSpeech[] | null> | null = null;

export function usePartsOfSpeech(writingSystemService: WritingSystemService): Readable<LabeledPartOfSpeech[]> {
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
  return derived([partsOfSpeechStore], ([partsOfSpeech]) => {
    return (partsOfSpeech ?? []).map(partOfSpeech => ({
      ...partOfSpeech,
      label: writingSystemService.pickBestAlternative(partOfSpeech.name, 'analysis'),
    }));
  });
}
