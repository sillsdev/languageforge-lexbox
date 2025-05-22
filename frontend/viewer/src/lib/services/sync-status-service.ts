import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import {type ProjectContext, useProjectContext} from '$lib/project-context.svelte';

export function useSyncStatusService() {
  const projectContext = useProjectContext();
  return new SyncStatusService(projectContext);
}

export class SyncStatusService {
  #projectContext: ProjectContext;
  get syncStatusApi(): ISyncServiceJsInvokable | undefined {
    return this.#projectContext.syncService;
  }

  constructor(projectContext: ProjectContext) {
    this.#projectContext = projectContext;
  }

  getStatus() {
    return this.syncStatusApi?.getSyncStatus();
  }

  getLocalStatus() {
    return this.syncStatusApi?.countPendingCrdtCommits();
  }

  triggerFwHeadlessSync() {
    return this.syncStatusApi?.triggerFwHeadlessSync();
  }
}
