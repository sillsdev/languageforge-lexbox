import {derived, type Readable, type Writable, writable} from 'svelte/store';
import type {SemanticDomain} from './mini-lcm';
import {useLexboxApi} from './services/service-provider';

let semanticDomainsStore: Writable<SemanticDomain[] | null> | null = null;
export function useSemanticDomains(): Readable<SemanticDomain[]> {
  if (semanticDomainsStore === null) {
    semanticDomainsStore = writable<SemanticDomain[] | null>(null, (set) => {
      useLexboxApi().GetSemanticDomains().then(semanticDomains => {
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
