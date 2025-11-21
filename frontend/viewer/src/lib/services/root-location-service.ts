import {getContext, hasContext, setContext} from 'svelte';

import type {useLocation} from 'svelte-routing';

const symbol = Symbol.for('fw-lite-location');
type RootLocation = ReturnType<typeof useLocation>;

export function initRootLocation(location: RootLocation): RootLocation {
  if (hasContext(symbol)) {
    if (import.meta.env.DEV) {
      throw new Error('RootLocation already initialized');
    }
    console.warn('RootLocation already initialized');
    return getContext(symbol);
  }
  setContext(symbol, location);
  return location;
}

export function useRootLocation(): RootLocation {
  if (!hasContext(symbol)) throw new Error('RootLocation not initialized');
  return getContext(symbol);
}
