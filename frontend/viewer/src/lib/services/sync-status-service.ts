// import { DotnetService, type IEntry, type IExampleSentence, type ISense } from '$lib/dotnet-types';
import { DotnetService } from '$lib/dotnet-types';
import type { ISyncServiceJsInvokable } from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
import { getContext } from 'svelte';
import type { IProjectSyncStatus } from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';

export function useSyncStatusService() {
  const projectName = getContext<string>('project-name');
  return new SyncStatusService(projectName);
}

export class SyncStatusService {
  get syncStatusApi(): ISyncServiceJsInvokable | undefined {
    if (import.meta.env.DEV) {
      //randomly return undefined to test fallback
      if (Math.random() < 0.5) {
        return undefined;
      }
    }
    return window.lexbox.ServiceProvider.tryGetService(DotnetService.SyncService);
  }

  constructor(private projectName: string) {
    // TODO: Do we need a constructor?
  }

  async getStatus(projectId: string) {
    const data = await (this.syncStatusApi?.getSyncStatus() ?? fetch(`/api/fw-lite/sync/status/{projectId}`)
      .then(res => res.json()));
    if (!data) {
      console.error('Invalid syncStatus data', data);
      return undefined;
    }
    return data as Promise<IProjectSyncStatus>;
  }
}
