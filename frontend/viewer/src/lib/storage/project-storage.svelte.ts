import {getContext, setContext} from 'svelte';

import type {IPreferencesService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {StorageProp} from './storage-prop.svelte';
import {usePreferencesService} from '$lib/services/service-provider';

const projectStorageContextKey = Symbol('project-storage');

export function initProjectStorage(projectCode: string): ProjectStorage {
  let storage = getContext<ProjectStorage>(projectStorageContextKey);
  if (storage) throw new Error('ProjectStorage already initialized');

  const backend = usePreferencesService();
  storage = new ProjectStorage(projectCode, backend);
  setContext(projectStorageContextKey, storage);
  return storage;
}

export function useProjectStorage(): ProjectStorage {
  const storage = getContext<ProjectStorage>(projectStorageContextKey);
  if (!storage) throw new Error('ProjectStorage not initialized. Make sure to call initProjectStorage() in a parent component.');
  return storage;
}

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
