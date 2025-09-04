<script lang="ts" module>
  export const SYNC_DIALOG_QUERY_PARAM = 'syncDialogOpen';
</script>

<script lang="ts">
  import type {IProjectSyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import {Dialog, DialogContent, DialogHeader, DialogTitle} from '$lib/components/ui/dialog';
  import {plural, t} from 'svelte-i18n-lingui';
  import {AppNotification} from '$lib/notifications/notifications';
  import {QueryParamStateBool} from '$lib/utils/url.svelte';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {watch} from 'runed';
  import {delay} from '$lib/utils/time';
  import {useFeatures} from '$lib/services/feature-service';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';
  import {useProjectContext} from '$lib/project-context.svelte';
  import SyncStatusPrimitive from './sync/SyncStatusPrimitive.svelte';

  const {
    syncStatus = SyncStatus.Success
  }: { syncStatus?: SyncStatus } = $props();

  const projectContext = useProjectContext();
  const service = useSyncStatusService();
  const features = useFeatures();
  let remoteStatus = $state<IProjectSyncStatus>();
  let localStatus = $state<IPendingCommits>();
  let server = $derived(projectContext.server);
  const openQueryParam = new QueryParamStateBool(
    { key: SYNC_DIALOG_QUERY_PARAM, replaceOnDefaultValue: true, allowBack: true },
    false,
  );
  let latestCommitDate = $state<string>();

  watch(() => openQueryParam.current, (newValue) => {
    if (newValue) void onOpen();
    else setTimeout(onClose, 500); // don't clear contents until close animation is done
  });

  export function open(): void {
    openQueryParam.current = true;
  }

  async function onOpen(): Promise<void> {
    await Promise.all([
      service.getLocalStatus().then(s => localStatus = s),
      service.getStatus().then(s => remoteStatus = s),
      service.getLatestCommitDate().then(s => latestCommitDate = s),
      service.getCurrentServer().then(s => server = s),
    ]);
  }

  function onClose(): void {
    localStatus = undefined;
    remoteStatus = undefined;
    latestCommitDate = undefined;
  }

  async function syncLexboxToFlex() {
    let safeToCloseDialog = false;

    const syncPromise = service.triggerFwHeadlessSync();
    AppNotification.promise(syncPromise, {
      loading: $t`Synchronizing FieldWorks Lite with FieldWorks...`,
      success: (result) => {
        const fwdataChangesText = $plural(result.syncResult?.fwdataChanges ?? 0, {one: '# change', other: '# changes'});
        const crdtChangesText = $plural(result.syncResult?.crdtChanges ?? 0, {one: '# change', other: '# changes'});
        return $t`${fwdataChangesText} synced to FieldWorks. ${crdtChangesText} synced to FieldWorks Lite.`;
      },
      error: (error) => $t`Failed to synchronize.` + '\n' + (error as Error).message,
      // TODO: Custom component that can expand or collapse the stacktrace
    });
    safeToCloseDialog = await syncPromise.then(() => true).catch(() => false);


    // Optimistically update status, then query it
    if (remoteStatus) {
      remoteStatus.pendingCrdtChanges = 0;
      remoteStatus.pendingMercurialChanges = 0;
    }
    // Auto-close dialog after successful FieldWorks sync
    await Promise.all([
      service.getStatus().then(s => remoteStatus = s),
      delay(750)
    ]);
    if (safeToCloseDialog && remoteStatus?.pendingMercurialChanges === 0 && remoteStatus?.pendingCrdtChanges === 0) {
      openQueryParam.current = false;
    }
  }


  async function syncLexboxToLocal() {
    const result = await service.triggerCrdtSync();
    if (!result) {
      AppNotification.display($t`Failed to synchronize`, 'error');
      return;
    }
    // Optimistically update status, then query it
    if (localStatus) {
      localStatus.remote = 0;
      localStatus.local = 0;
    }
    await Promise.all([
      service.getLocalStatus().then(s => localStatus = s),
      service.getStatus().then(s => remoteStatus = s),
      service.getLatestCommitDate().then(s => latestCommitDate = s),
    ]);
  }

  function onLoginStatusChange(status: 'logged-in' | 'logged-out') {
    if (status === 'logged-in') {
      void syncLexboxToLocal();
    }
  }
</script>

<Dialog bind:open={openQueryParam.current}>
  <DialogContent class="sm:min-h-80 sm:min-w-[35rem] grid-rows-[auto_1fr] items-center">
    <DialogHeader>
      <DialogTitle>{$t`Sync Changes`}</DialogTitle>
    </DialogHeader>
    <SyncStatusPrimitive
      {syncStatus}
      {remoteStatus}
      {localStatus}
      {server}
      projectCode={projectContext.projectData?.code}
      serverId={projectContext.projectData?.serverId}
      {latestCommitDate}
      canSyncLexboxToFlex={features?.write}
      {syncLexboxToFlex}
      {syncLexboxToLocal}
      {onLoginStatusChange}
    />
  </DialogContent>
</Dialog>
