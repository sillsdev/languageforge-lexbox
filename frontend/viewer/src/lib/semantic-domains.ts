import {derived, type Readable, type Writable, writable} from 'svelte/store';
import type {ISemanticDomain} from '$lib/dotnet-types';
import {useLexboxApi} from './services/service-provider';

let semanticDomainsStore: Writable<ISemanticDomain[] | null> | null = null;
export function useSemanticDomains(): Readable<ISemanticDomain[]> {
  if (semanticDomainsStore === null) {
    semanticDomainsStore = writable<ISemanticDomain[] | null>(null, (set) => {
      useLexboxApi().getSemanticDomains().then(semanticDomains => {
        set(semanticDomains);
      }).catch(error => {
        console.error('Failed to load semantic domains', error);
        throw error;
      });
    });
  }
  return derived(semanticDomainsStore, (semanticDomains) => {
    return semanticDomains ?? [];
  });
}
