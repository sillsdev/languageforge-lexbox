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
  import {CoalescedRefresh} from '$lib/utils/coalesced-refresh';
  import {useFeatures} from '$lib/services/feature-service';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';
  import {useProjectContext} from '$project/project-context.svelte';
  import SyncStatusPrimitive from './sync/SyncStatusPrimitive.svelte';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import {useProjectEventBus} from '$lib/services/event-bus';

  const {
    syncStatus = SyncStatus.Success
  }: { syncStatus?: SyncStatus } = $props();

  const projectContext = useProjectContext();
  const service = useSyncStatusService();
  const features = useFeatures();
  const projectEventBus = useProjectEventBus();
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
  });

  export function open(): void {
    openQueryParam.current = true;
  }

  // Local status is quick; the remote (FieldWorks Classic) status is a Lexbox request that can take many seconds,
  // so they refresh independently and nothing that only needs local data waits on the server.
  const localRefresh = new CoalescedRefresh(async () => {
    await Promise.all([
      service.getLocalStatus().then(s => localStatus = s),
      service.getLatestSyncedCommitDate().then(s => latestSyncedCommitDate = s),
    ]);
  });
  const remoteRefresh = new CoalescedRefresh(async () => {
    remoteStatus = await service.getStatus();
  });

  // Catch background syncs (e.g. another client pushing changes) that onOpen alone wouldn't see.
  // forceFresh: an in-flight refresh started before the sync may return pre-sync data, so queue a follow-up.
  // Must follow the refreshers — onSync invokes the callback synchronously with a cached event.
  projectEventBus.onSync(e => {
    if (openQueryParam.current && e.status === SyncStatus.Success) {
      void localRefresh.run({forceFresh: true});
      void remoteRefresh.run({forceFresh: true});
    }
  });

  async function onOpen(): Promise<void> {
    await Promise.all([
      localRefresh.run(),
      remoteRefresh.run(),
      service.getCurrentServer().then(s => server = s),
    ]);
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
      void remoteRefresh.run({forceFresh: true});
      throw error;
    }

    // Auto-close dialog after successful FieldWorks sync
    await Promise.all([
      remoteRefresh.run({forceFresh: true}),
      delay(750)
    ]);
    if (safeToCloseDialog && remoteStatus?.pendingMercurialChanges === 0 && remoteStatus?.pendingCrdtChanges === 0) {
      openQueryParam.current = false;
    }
  }


  async function syncLexboxToLocal() {
    const result = await service.triggerCrdtSync();
    if (!result.isSynced) {
      // Couldn't sync (offline, server unreachable, or not logged in): the backend has already published the
      // real sync status, which re-renders this dialog, so give gentle feedback instead of optimistically
      // zeroing the counts.
      AppNotification.display($t`Couldn't sync right now — your local changes are safe.`, {type: 'warning', timeout: 'short'});
      void remoteRefresh.run();
      await localRefresh.run();
      return;
    }
    // Optimistically update status, then query it
    if (localStatus) {
      localStatus.remote = 0;
      localStatus.local = 0;
    }
    // Don't hold the sync button on the slow remote status; its counts fill in when the server answers
    void remoteRefresh.run({forceFresh: true});
    await localRefresh.run({forceFresh: true});
  }

  function onLoginStatusChange(status: 'logged-in' | 'logged-out') {
    if (status === 'logged-in') {
      void syncLexboxToLocal();
    }
  }
</script>

<ResponsiveDialog bind:open={openQueryParam.current} disableBackHandler title={$t`Sync Changes`}
  contentProps={{ class: 'sm:min-w-140 grid-rows-[auto_1fr] items-center' }}>
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
