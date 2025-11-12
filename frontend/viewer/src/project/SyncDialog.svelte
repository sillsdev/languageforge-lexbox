<script lang="ts" module>
  export const SYNC_DIALOG_QUERY_PARAM = 'syncDialogOpen';
</script>

<script lang="ts">
  import type {IProjectSyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import {plural, t} from 'svelte-i18n-lingui';
  import {AppNotification} from '$lib/notifications/notifications';
  import {QueryParamStateBool} from '$lib/utils/url.svelte';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {watch} from 'runed';
  import {delay} from '$lib/utils/time';
  import {useFeatures} from '$lib/services/feature-service';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';
  import {useProjectContext} from '$project/project-context.svelte';
  import SyncStatusPrimitive from './sync/SyncStatusPrimitive.svelte';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';

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
  let latestSyncedCommitDate = $state<string>();

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
      service.getLatestSyncedCommitDate().then(s => latestSyncedCommitDate = s),
      service.getCurrentServer().then(s => server = s),
    ]);
  }

  function onClose(): void {
    localStatus = undefined;
    remoteStatus = undefined;
    latestSyncedCommitDate = undefined;
  }

  async function syncLexboxToFlex() {
    let safeToCloseDialog = false;

    const syncPromise = service.triggerFwHeadlessSync();
    AppNotification.promise(syncPromise, {
      loading: $t`Synchronizing FieldWorks Lite and FieldWorks Classic...`,
      success: (result) => {
        const fwdataChangesText = $plural(result.syncResult?.fwdataChanges ?? 0, {one: '# change', other: '# changes'});
        const crdtChangesText = $plural(result.syncResult?.crdtChanges ?? 0, {one: '# change', other: '# changes'});
        return $t`Sync complete. ${fwdataChangesText} were applied to FieldWorks Classic. ${crdtChangesText} were applied to FieldWorks Lite.`;
      },
      // TODO: Custom component that can expand or collapse the stacktrace
    });

    try {
      await syncPromise;
      safeToCloseDialog = true;
      if (remoteStatus) {
        // Optimistically update status, then query it
        remoteStatus.pendingCrdtChanges = 0;
        remoteStatus.pendingMercurialChanges = 0;
      }
    } catch (error) {
      safeToCloseDialog = false;
      void service.getStatus().then(s => remoteStatus = s);
      throw error;
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
      service.getLatestSyncedCommitDate().then(s => latestSyncedCommitDate = s),
    ]);
  }

  function onLoginStatusChange(status: 'logged-in' | 'logged-out') {
    if (status === 'logged-in') {
      void syncLexboxToLocal();
    }
  }
</script>

<ResponsiveDialog bind:open={openQueryParam.current} disableBackHandler title={$t`Sync Changes`}
  contentProps={{ class: 'sm:min-w-[35rem] grid-rows-[auto_1fr] items-center' }}>
  <SyncStatusPrimitive
    {syncStatus}
    {remoteStatus}
    {localStatus}
    {server}
    projectCode={projectContext.projectData?.code}
    serverId={projectContext.projectData?.serverId}
    latestSyncedCommitDate={latestSyncedCommitDate}
    canSyncLexboxToFlex={features?.write}
    {syncLexboxToFlex}
    {syncLexboxToLocal}
    {onLoginStatusChange}
  />
</ResponsiveDialog>
