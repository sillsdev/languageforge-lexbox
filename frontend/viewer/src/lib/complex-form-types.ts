﻿import {derived, type Readable, type Writable, writable} from 'svelte/store';
import type {ComplexFormType} from './mini-lcm';
import {useLexboxApi} from './services/service-provider';

let complexFormTypesStore: Writable<ComplexFormType[] | null> | null = null;
export function useComplexFormTypes(): Readable<ComplexFormType[]> {
  if (complexFormTypesStore === null) {
    complexFormTypesStore = writable<ComplexFormType[] | null>(null, (set) => {
      useLexboxApi().GetComplexFormTypes().then(complexFormTypes => {
        set(complexFormTypes);
      });
    });
  }
  return derived(complexFormTypesStore, (complexFormTypes) => {
    return complexFormTypes ?? [];
  });
}