import {getContext, setContext} from 'svelte';

import type {IPreferencesService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {StorageProp} from './storage-prop.svelte';
import {usePreferencesService} from '$lib/services/service-provider';
import {useProjectContext} from '$project/project-context.svelte';

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
    const backend = usePreferencesService();
    storage = new ProjectStorage(projectContext.projectCode, backend);
    setContext(projectStorageContextKey, storage);
  }
  return storage;
}
