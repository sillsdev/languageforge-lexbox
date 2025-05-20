import type { ISyncServiceJsInvokable } from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import type { IProjectSyncStatus } from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
import { ProjectContext, useProjectContext } from '$lib/project-context.svelte';

export function useSyncStatusService() {
  const projectContext = useProjectContext();
  return new SyncStatusService(projectContext);
}

export class SyncStatusService {
  #projectContext: ProjectContext;
  get syncStatusApi(): ISyncServiceJsInvokable | undefined {
    return this.#projectContext.syncService;
  }

  constructor(private projectContext: ProjectContext) {
    this.#projectContext = projectContext;
  }

  async getStatus() {
    const data = this.syncStatusApi?.getSyncStatus();
    if (!data) {
      return undefined;
    }
    return data as Promise<IProjectSyncStatus>;
  }
}
