import {getContext, hasContext, setContext} from 'svelte';

import {useLocation} from 'svelte-routing';

const symbol = Symbol.for('fw-lite-location');

export function initRootLocation(): ReturnType<typeof useLocation> {
  if (hasContext(symbol)) {
    if (import.meta.env.DEV) {
      throw new Error('RootLocation already initialized');
    }
    console.warn('RootLocation already initialized');
    return getContext(symbol);
  }
  const locationStore = useLocation();
  setContext(symbol, locationStore);
  return locationStore;
}

export function useRootLocation(): ReturnType<typeof useLocation> {
  if (!hasContext(symbol)) throw new Error('RootLocation not initialized');
  return getContext(symbol);
}
