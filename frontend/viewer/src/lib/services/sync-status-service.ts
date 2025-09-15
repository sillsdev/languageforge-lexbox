import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import {SyncJobStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncJobStatusEnum';
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

  async triggerFwHeadlessSync() {
    const result = await this.syncStatusApi.triggerFwHeadlessSync();
    if (result.status === SyncJobStatusEnum.Success) return result;
    else throw new Error(result.error as string ?? `Sync failed with status ${result.status} but no error message`, {cause: 'TODO: Exception should go here'});
    // TODO: Tweak SyncJobResult to have an error *message* and error *details*, and put the details in the `cause` property of the JS Error that we throw
    // throw new Error(result.errorMessage, {cause: result.errorDetails});

  }

  getLatestSyncedCommitDate() {
    return this.syncStatusApi.getLatestSyncedCommitDate();
  }

  getCurrentServer() {
    return this.syncStatusApi.getCurrentServer();
  }
}
