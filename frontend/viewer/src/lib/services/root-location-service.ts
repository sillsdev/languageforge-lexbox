import {createContext} from 'svelte';

import type {useLocation} from 'svelte-routing';

type RootLocation = ReturnType<typeof useLocation>;
const [getRootLocation, setRootLocation] = createContext<RootLocation>();

export function initRootLocation(location: RootLocation): RootLocation {
  const existingLocation = getRootLocation();
  if (existingLocation) {
    if (import.meta.env.DEV) {
      throw new Error('RootLocation already initialized');
    }
    console.warn('RootLocation already initialized');
    return existingLocation;
  }
  setRootLocation(location);
  return location;
}

export function useRootLocation(): RootLocation {
  const location = getRootLocation();
  if (!location) throw new Error('RootLocation not initialized');
  return location;
}
