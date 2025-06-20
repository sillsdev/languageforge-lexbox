import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import {type ProjectContext, useProjectContext} from '$lib/project-context.svelte';

export function useSyncStatusService() {
  const projectContext = useProjectContext();
  if (!projectContext.syncService) {
    throw new Error('SyncService not available in the current project context');
  }
  return new SyncStatusService(projectContext);
}

export class SyncStatusService {
  #projectContext: ProjectContext;
  get syncStatusApi(): ISyncServiceJsInvokable {
    if (!this.#projectContext.syncService) {
      throw new Error('SyncService not available in the current project context');
    }
    return this.#projectContext.syncService;
  }

  constructor(projectContext: ProjectContext) {
    this.#projectContext = projectContext;
  }

  getStatus() {
    return this.syncStatusApi.getSyncStatus();
  }

  getLocalStatus() {
    return this.syncStatusApi.countPendingCrdtCommits();
  }

  triggerCrdtSync(skipNotifications = false) {
    return this.syncStatusApi.executeSync(skipNotifications);
  }

  triggerFwHeadlessSync() {
    return this.syncStatusApi.triggerFwHeadlessSync();
  }

  getLatestCommitDate() {
    return this.syncStatusApi.getLatestCommitDate();
  }

  getCurrentServer() {
    return this.syncStatusApi.getCurrentServer();
  }
}
