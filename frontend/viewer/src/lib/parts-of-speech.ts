﻿import {derived, type Readable, type Writable, writable} from 'svelte/store';
import type {PartOfSpeech} from './mini-lcm';
import {useLexboxApi} from './services/service-provider';

let partsOfSpeechStore: Writable<PartOfSpeech[] | null> | null = null;

export function usePartsOfSpeech(): Readable<PartOfSpeech[]> {
  if (partsOfSpeechStore === null) {
    partsOfSpeechStore = writable<PartOfSpeech[] | null>([], (set) => {
      useLexboxApi().GetPartsOfSpeech().then(partsOfSpeech => {
        set(partsOfSpeech);
      });
    });
  }
  return derived(partsOfSpeechStore, (partsOfSpeech) => {
    return partsOfSpeech ?? [];
  });
}
