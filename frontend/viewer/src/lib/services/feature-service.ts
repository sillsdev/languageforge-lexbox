import type {IMiniLcmFeatures} from '$lib/dotnet-types';
import {getContext, setContext} from 'svelte';
import {type Readable, type Writable, writable} from 'svelte/store';

const featureContextName = 'features';

export type LexboxFeatures = IMiniLcmFeatures;

export function initFeatures(defaultFeatures: LexboxFeatures): Writable<LexboxFeatures> {
  const featureStore = writable<LexboxFeatures>(defaultFeatures);
  setContext<Readable<LexboxFeatures>>(featureContextName, featureStore);
  return featureStore;
}

export function useFeatures(): Readable<LexboxFeatures> {
  return getContext<Readable<LexboxFeatures>>(featureContextName);
}
