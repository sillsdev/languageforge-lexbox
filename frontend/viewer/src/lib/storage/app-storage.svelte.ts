import {PreferenceKey} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {StorageProp} from './storage-prop.svelte';
import {usePreferencesService} from '$lib/services/service-provider';

class AppStorage {
  readonly lastUrl: StorageProp;

  constructor() {
    const backend = usePreferencesService();
    this.lastUrl = new StorageProp(PreferenceKey.AppLastUrl, backend);
  }
}

let instance: AppStorage | undefined;

export function useAppStorage(): AppStorage {
  instance ??= new AppStorage();
  return instance;
}
