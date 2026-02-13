import {StorageProp, getPreferencesService} from './project-storage.svelte';

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
