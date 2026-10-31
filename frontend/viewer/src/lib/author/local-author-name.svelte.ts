import {PreferenceKey} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {StorageProp} from '$lib/storage/storage-prop.svelte';
import {usePreferencesService} from '$lib/services/service-provider';

/**
 * The name this device signs commits and comments with when no Lexbox user is known.
 * Loads from the device preferences on creation, so create one per component and pass it to the
 * {@link LocalAuthorNameDialog} that edits it.
 */
export function useLocalAuthorName(): StorageProp {
  return new StorageProp(PreferenceKey.LocalAuthorName, usePreferencesService());
}
