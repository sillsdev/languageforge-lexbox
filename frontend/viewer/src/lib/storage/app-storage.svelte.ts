import {getContext, setContext} from 'svelte';

import {PreferenceKey, type IPreferencesService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {StorageProp} from './storage-prop.svelte';
import {usePreferencesService} from '$lib/services/service-provider';

class AppStorage {
  readonly lastUrl: StorageProp;

  constructor(backend: IPreferencesService) {
    this.lastUrl = new StorageProp(PreferenceKey.AppLastUrl, backend);
  }
}

const appStorageContextKey = Symbol('app-storage');

export function initAppStorage(): AppStorage {
  let storage = getContext<AppStorage>(appStorageContextKey);
  if (storage) throw new Error('AppStorage already initialized');

  const backend = usePreferencesService();
  storage = new AppStorage(backend);
  setContext(appStorageContextKey, storage);
  return storage;
}

export function useAppStorage(): AppStorage {
  const storage = getContext<AppStorage>(appStorageContextKey);
  if (!storage) throw new Error('AppStorage not initialized. Make sure to call initAppStorage() in a parent component.');
  return storage;
}
