import {PreferenceKey, type IPreferencesService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {usePreferencesService} from '$lib/services/service-provider';
import {StorageProp} from '$lib/storage/storage-prop.svelte';

let cached: {backend: IPreferencesService, name: StorageProp} | undefined;

/**
 * The device-wide name the backend signs changes with in projects that have no Lexbox user yet. Empty until the user enters one.
 */
export function useLocalAuthorName(): StorageProp {
  const backend = usePreferencesService();
  if (cached?.backend !== backend) cached = {backend, name: new StorageProp(PreferenceKey.LocalAuthorName, backend)};
  return cached.name;
}
