import {SyncJobStatusEnum, type ISyncResult} from '$lib/dotnet-types';
import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import {gt} from 'svelte-i18n-lingui';

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

  async triggerFwHeadlessSync(): Promise<{status: SyncJobStatusEnum.Success, syncResult?: ISyncResult}> {
    const result = await this.syncStatusApi.triggerFwHeadlessSync();
    if (result.status === SyncJobStatusEnum.Success && !result.error) return {status: SyncJobStatusEnum.Success, syncResult: result.syncResult};
    else {
      const syncError = result.error as string ?? `Sync failed with status ${result.status} but no error message`;
      throw new Error(gt`Failed to synchronize` + `\n${syncError}`);
    }
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
