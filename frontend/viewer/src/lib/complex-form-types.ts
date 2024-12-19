import {derived, type Readable, type Writable, writable} from 'svelte/store';
import type {IComplexFormType} from '$lib/dotnet-types';
import {useLexboxApi} from './services/service-provider';

let complexFormTypesStore: Writable<IComplexFormType[] | null> | null = null;
export function useComplexFormTypes(): Readable<IComplexFormType[]> {
  if (complexFormTypesStore === null) {
    complexFormTypesStore = writable<IComplexFormType[] | null>(null, (set) => {
      useLexboxApi().getComplexFormTypes().then(complexFormTypes => {
        set(complexFormTypes);
      }).catch(error => {
        console.error('Failed to load parts of speech', error);
        throw error;
      });
    });
  }
  return derived(complexFormTypesStore, (complexFormTypes) => {
    return complexFormTypes ?? [];
  });
}
