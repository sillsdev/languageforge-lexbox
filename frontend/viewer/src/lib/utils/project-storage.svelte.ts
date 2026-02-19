import {getContext, setContext} from 'svelte';

import type {IPreferencesService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {useProjectContext} from '$project/project-context.svelte';
import {StorageProp, getPreferencesService} from './storage-prop.svelte';

/**
 * Project-specific storage service
 *
 * This service provides project-scoped storage for user preferences.
 * When running in MAUI, uses MAUI Preferences via the PreferencesService.
 * Otherwise, falls back to localStorage.
 */

const projectStorageContextKey = 'project-storage';

/**
 * A StorageProp scoped to a specific project.
 * Keys are automatically prefixed with `project:{projectCode}:`.
 */
class ProjectStorageProp extends StorageProp {
  constructor(projectCode: string, key: string, backend: IPreferencesService) {
    super(`project:${projectCode}:${key}`, backend);
  }
}

export class ProjectStorage {
  readonly selectedTaskId: ProjectStorageProp;

  constructor(projectCode: string, backend: IPreferencesService) {
    this.selectedTaskId = new ProjectStorageProp(projectCode, 'selectedTaskId', backend);
  }
}

export function useProjectStorage(): ProjectStorage {
  let storage = getContext<ProjectStorage>(projectStorageContextKey);
  if (!storage) {
    const projectContext = useProjectContext();
    const backend = getPreferencesService();
    storage = new ProjectStorage(projectContext.projectCode, backend);
    setContext(projectStorageContextKey, storage);
  }
  return storage;
}
