import {StorageProp, getPreferencesService} from './storage-prop.svelte';

class AppStorage {
  readonly lastUrl: StorageProp;

  constructor() {
    const backend = getPreferencesService();
    this.lastUrl = new StorageProp('app:lastUrl', backend);
  }
}

let instance: AppStorage | undefined;

export function useAppStorage(): AppStorage {
  instance ??= new AppStorage();
  return instance;
}
