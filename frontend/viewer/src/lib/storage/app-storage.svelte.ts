import {createContext} from 'svelte';

import {PreferenceKey, type IPreferencesService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {StorageProp} from './storage-prop.svelte';
import {usePreferencesService} from '$lib/services/service-provider';

const [getAppStorage, setAppStorage] = createContext<AppStorage>();

export function initAppStorage(): AppStorage {
  let storage = getAppStorage();
  if (storage) throw new Error('AppStorage already initialized');

  const backend = usePreferencesService();
  storage = new AppStorage(backend);
  setAppStorage(storage);
  return storage;
}

export function useAppStorage(): AppStorage {
  const storage = getAppStorage();
  if (!storage) throw new Error('AppStorage not initialized. Make sure to call initAppStorage() in a parent component.');
  return storage;
}

export class AppStorage {
  readonly lastUrl: StorageProp;

  constructor(backend: IPreferencesService) {
    this.lastUrl = new StorageProp(PreferenceKey.AppLastUrl, backend);
  }
}
