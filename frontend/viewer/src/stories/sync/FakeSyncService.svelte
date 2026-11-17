<script lang="ts" module>
  import type {IProjectSyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';
  import type {IFolderSyncSummary} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/IFolderSyncSummary';

  export interface FakeSyncData {
    localStatus?: IPendingCommits;
    latestSyncedCommitDate?: string;
    /** Left out: the remote status never loads */
    remoteStatus?: IProjectSyncStatus;
    rememberedSyncFolder?: string;
    folderSyncResult?: IFolderSyncSummary;
  }
</script>

<script lang="ts">
  import {untrack, type Snippet} from 'svelte';
  import type {ISyncServiceJsInvokable} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/ISyncServiceJsInvokable';
  import {SyncJobStatusEnum, type ILexboxServer} from '$lib/dotnet-types';
  import {FolderSyncStatus} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/FolderSyncStatus';
  import {initProjectContext, useProjectContext} from '$project/project-context.svelte';
  import {delay} from '$lib/utils/time';

  const {data, server, fieldWorksSync, children}: {
    data: FakeSyncData;
    /** The Lexbox server the project syncs with; the sync dialog shows its Lexbox tab only with one */
    server?: ILexboxServer;
    /** Set to report the FieldWorks Classic sync feature, which the demo project doesn't */
    fieldWorksSync?: boolean;
    children: Snippet;
  } = $props();

  const syncService: ISyncServiceJsInvokable = {
    getSyncStatus: () => data.remoteStatus ? Promise.resolve(data.remoteStatus) : new Promise(() => {}),
    triggerFwHeadlessSync: () => delay(1000).then(() => ({status: SyncJobStatusEnum.Success})),
    countPendingCrdtCommits: () => Promise.resolve(data.localStatus),
    getLatestSyncedCommitDate: () => Promise.resolve(data.latestSyncedCommitDate),
    executeSync: () => delay(1000).then(() => ({isSynced: true, missingFromLocal: [], missingFromRemote: []})),
    getCurrentServer: () => Promise.resolve(server),
    syncWithFolder: () => delay(1000).then(() => data.folderSyncResult ?? {status: FolderSyncStatus.Synced, mediaSent: 2, mediaReceived: 3, mediaFailed: 0}),
    getRememberedSyncFolder: () => Promise.resolve(data.rememberedSyncFolder),
  };

  // Nested over the demo project's context, adding the sync service the demo doesn't have
  const demoContext = useProjectContext();
  // a story's props don't change, so the context is set up once from their first values
  const initial = untrack(() => ({server, fieldWorksSync}));
  const api = initial.fieldWorksSync === undefined ? demoContext.api : new Proxy(demoContext.api, {
    get(target, prop) {
      if (prop === 'supportedFeatures') {
        return async () => ({...await target.supportedFeatures(), sync: true, fieldWorksSync: initial.fieldWorksSync});
      }
      // bound, because the demo API keeps its state in private fields that a proxied `this` can't reach
      const value: unknown = Reflect.get(target, prop);
      return typeof value === 'function' ? (value as (...args: unknown[]) => unknown).bind(target) : value;
    },
  });
  initProjectContext({
    api,
    projectName: demoContext.projectName,
    projectCode: demoContext.projectCode,
    server: initial.server,
    syncService,
  });
</script>

{@render children()}
