<script lang="ts" module>
  import type {IProjectSyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';

  export interface FakeSyncData {
    localStatus?: IPendingCommits;
    latestSyncedCommitDate?: string;
    /** Left out: the remote status never loads */
    remoteStatus?: IProjectSyncStatus;
    rememberedSyncFolder?: string;
  }
</script>

<script lang="ts">
  import type {Snippet} from 'svelte';
  import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
  import {SyncJobStatusEnum} from '$lib/dotnet-types';
  import {FolderSyncStatus} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/FolderSyncStatus';
  import {initProjectContext, useProjectContext} from '$project/project-context.svelte';
  import {delay} from '$lib/utils/time';

  const {data, children}: {data: FakeSyncData; children: Snippet} = $props();

  const syncService: ISyncServiceJsInvokable = {
    getSyncStatus: () => data.remoteStatus ? Promise.resolve(data.remoteStatus) : new Promise(() => {}),
    triggerFwHeadlessSync: () => delay(1000).then(() => ({status: SyncJobStatusEnum.Success})),
    countPendingCrdtCommits: () => Promise.resolve(data.localStatus),
    getLatestSyncedCommitDate: () => Promise.resolve(data.latestSyncedCommitDate),
    executeSync: () => delay(1000).then(() => ({isSynced: true, missingFromLocal: [], missingFromRemote: []})),
    getCurrentServer: () => Promise.resolve(undefined),
    syncWithFolder: () => delay(1000).then(() => ({status: FolderSyncStatus.Synced, folderPath: String.raw`E:\FieldWorks Lite\Example project`, mediaSent: 2, mediaReceived: 3, mediaFailed: 0})),
    getRememberedSyncFolder: () => Promise.resolve(data.rememberedSyncFolder),
  };

  // Nested over the demo project's context, adding the sync service the demo doesn't have
  const demoContext = useProjectContext();
  initProjectContext({
    api: demoContext.api,
    projectName: demoContext.projectName,
    projectCode: demoContext.projectCode,
    syncService,
  });
</script>

{@render children()}
