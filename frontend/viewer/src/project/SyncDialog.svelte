<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import { Icon, PingingIcon } from '$lib/components/ui/icon';
  import type { IProjectSyncStatus } from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import { Dialog, DialogContent, DialogHeader, DialogTitle } from '$lib/components/ui/dialog';
  import { t, plural } from 'svelte-i18n-lingui';
  import { AppNotification } from '$lib/notifications/notifications';
  import { QueryParamStateBool } from '$lib/utils/url.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import { useSyncStatusService } from '$lib/services/sync-status-service';
  import { formatDate } from '$lib/components/ui/format';
  import { watch } from 'runed';
  import { fade } from 'svelte/transition';
  import { delay } from '$lib/utils/time';
  import { cn } from '$lib/utils';
  import {useFeatures} from '$lib/services/feature-service';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import {useProjectContext} from '$lib/project-context.svelte';
  import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
  import {ProjectSyncStatusErrorCode} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusErrorCode';

  const {
    syncStatus = SyncStatus.Success
  }: {syncStatus?: SyncStatus} = $props();

  const projectContext = useProjectContext();
  const service = useSyncStatusService();
  const features = useFeatures();
  let remoteStatus = $state<IProjectSyncStatus>();
  let localStatus = $state<IPendingCommits>();
  let server = $derived(projectContext.server);
  let loading = $state(false);
  const openQueryParam = new QueryParamStateBool(
    { key: 'syncDialogOpen', replaceOnDefaultValue: true, allowBack: true },
    false,
  );

  let lbToLocalCount = $derived(localStatus?.remote);
  let localToLbCount = $derived(localStatus?.local);
  let latestCommitDate = $state<string>();
  let lastLocalSyncDate = $derived(latestCommitDate ? new Date(latestCommitDate) : undefined);
  const lastFlexSyncDate = $derived(remoteStatus?.lastMercurialCommitDate ? new Date(remoteStatus.lastMercurialCommitDate) : undefined);
  let lbToFlexCount = $derived(remoteStatus?.pendingCrdtChanges);
  let flexToLbCount = $derived(remoteStatus?.pendingMercurialChanges);
  const serverName = $derived(server?.displayName ?? projectContext.projectData?.serverId ?? 'unknown');

  watch(() => openQueryParam.current, (newValue) => {
    if (newValue) void onOpen();
    else setTimeout(onClose, 500); // don't clear contents until close animation is done
  });

  export function open(): void {
    openQueryParam.current = true;
  }

  async function onOpen(): Promise<void> {
    loading = true;
    try {
      let serverPromise = service.getCurrentServer();
      let remotePromise = service.getStatus();
      let localPromise = service.getLocalStatus();
      let commitDatePromise = service.getLatestCommitDate();
      [localStatus, remoteStatus, latestCommitDate, server] = await Promise.all([
        localPromise,
        remotePromise,
        commitDatePromise,
        serverPromise,
      ]);
    } finally {
      loading = false;
    }
  }

  function onClose(): void {
    localStatus = undefined;
    remoteStatus = undefined;
    latestCommitDate = undefined;
  }

  let loadingSyncLexboxToFlex = $state(false);
  async function syncLexboxToFlex() {
    loadingSyncLexboxToFlex = true;
    try {
      const syncPromise = service.triggerFwHeadlessSync();
      AppNotification.promise(syncPromise, {
        loading: $t`Synchronizing FieldWorks Lite with FieldWorks...`,
        success: (result) => {
          const fwdataChangesText = $plural(result.fwdataChanges, { one: '# change', other: '# changes' });
          const crdtChangesText = $plural(result.crdtChanges, { one: '# change', other: '# changes' });
          return $t`${fwdataChangesText} synced to FieldWorks. ${crdtChangesText} synced to FieldWorks Lite.`;
        },
        error: $t`Failed to synchronize.`,
      });
      await syncPromise;
    } finally {
      loadingSyncLexboxToFlex = false;
    }

    // Optimistically update status, then query it
    lbToFlexCount = 0;
    flexToLbCount = 0;
    const statusPromise = service.getStatus();
    // Auto-close dialog after successful FieldWorks sync
    [remoteStatus] = await Promise.all([statusPromise, delay(750)]);
    if (remoteStatus.pendingMercurialChanges === 0 && remoteStatus.pendingCrdtChanges === 0) {
      openQueryParam.current = false;
    }
  }

  let loadingSyncLexboxToLocal = $state(false);
  async function syncLexboxToLocal() {
    loadingSyncLexboxToLocal = true;
    try {
      const result = await service.triggerCrdtSync();
      if (!result) {
        AppNotification.display($t`Failed to synchronize`, 'error');
        return;
      }
      // Optimistically update status, then query it
      lbToLocalCount = 0;
      localToLbCount = 0;
      const statusPromise = service.getLocalStatus();
      const remoteStatusPromise = service.getStatus();
      const datePromise = service.getLatestCommitDate();
      [localStatus, remoteStatus, latestCommitDate] = await Promise.all([
        statusPromise,
        remoteStatusPromise,
        datePromise,
      ]);
    } finally {
      loadingSyncLexboxToLocal = false;
    }
  }
</script>

<Dialog bind:open={openQueryParam.current}>
  <DialogContent class="sm:min-h-80 sm:min-w-96 grid-rows-[auto_1fr] items-center">
    <DialogHeader>
      <DialogTitle>{$t`Synchronize`}</DialogTitle>
    </DialogHeader>
    <!-- remoteStatus always gets set, so it's only here for the compiler -->
    {#if loading || !remoteStatus}
      <Loading class="place-self-center size-10" />
    {:else}
      <!-- 1fr_7fr_1fr seems to be a reliable way to prevent the buttons states from resizing the dialog -->
      <div in:fade
        class="grid grid-rows-[auto] grid-cols-[1fr_7fr_1fr] gap-y-6 gap-x-8"
      >
        <div class="col-span-full text-center">
          <Icon icon="i-mdi-monitor-cellphone" class="size-10" />
        </div>
        <div class="text-center content-center">
          {lbToLocalCount ?? '?'}
          <PingingIcon
            icon="i-mdi-arrow-up"
            ping={loadingSyncLexboxToLocal && !!lbToLocalCount}
            class={cn(loadingSyncLexboxToLocal && !!lbToLocalCount && 'text-primary')}
          />
        </div>
        <div class="content-center text-center">
          {#if syncStatus === SyncStatus.Success}
            <Button
              variant="outline"
              class="border-primary text-primary hover:text-primary"
              loading={loadingSyncLexboxToLocal}
              disabled={loadingSyncLexboxToFlex}
              onclick={syncLexboxToLocal}
              icon="i-mdi-sync"
              iconProps={{ class: 'size-5' }}>
              {#if loadingSyncLexboxToLocal}
                {$t`Synchronizing...`}
              {:else}
                {$t`Auto synchronizing`}
              {/if}
            </Button>
          {:else if syncStatus === SyncStatus.Offline}
            <div><Icon icon="i-mdi-cloud-off-outline" /> {$t`Offline`}</div>
          {:else if syncStatus === SyncStatus.NotLoggedIn && projectContext.server}
            <LoginButton text={$t`Login`} status={{loggedIn: false, server: projectContext.server}} />
          {:else if syncStatus === SyncStatus.NoServer && projectContext.projectData?.serverId}
            <div>{$t`Unknown server: ${projectContext.projectData?.serverId}`}</div>
          {:else if syncStatus === SyncStatus.NoServer}
            <div>{$t`No server configured`}</div>
          {:else}
            <div class="text-destructive">{$t`Error getting sync status.`}</div>
          {/if}
        </div>
        <div class="text-center content-center">
          <PingingIcon
            icon="i-mdi-arrow-down"
            ping={loadingSyncLexboxToLocal && !!localToLbCount}
            class={cn(loadingSyncLexboxToLocal && !!localToLbCount && 'text-primary')}
          />
          {localToLbCount}
        </div>
        <div class="col-span-full text-center flex flex-col">
          <span class="font-medium">
            <Icon icon="i-mdi-cloud-outline" />
            {$t`${serverName} - FieldWorks Lite`}
          </span>
          <span class="text-foreground/80">
            {$t`Last change: ${formatDate(lastLocalSyncDate)}`}
          </span>
        </div>
        {#if server}
          <div class="text-center content-center">
            {flexToLbCount}
            <PingingIcon
              icon="i-mdi-arrow-up"
              ping={loadingSyncLexboxToFlex && !!flexToLbCount}
              class={cn(loadingSyncLexboxToFlex && !!flexToLbCount && 'text-primary')}
            />
          </div>
          <div class="content-center text-center">
            <Button
              loading={loadingSyncLexboxToFlex}
              disabled={loadingSyncLexboxToLocal || !features.write}
              onclick={syncLexboxToFlex}
              icon="i-mdi-sync"
              iconProps={{ class: 'size-5' }}>
              {#if loadingSyncLexboxToFlex}
                {$t`Synchronizing...`}
              {:else}
                {$t`Synchronize`}
              {/if}
            </Button>
          </div>
          <div class="text-center content-center">
            <PingingIcon
              icon="i-mdi-arrow-down"
              ping={loadingSyncLexboxToFlex && !!lbToFlexCount}
              class={cn(loadingSyncLexboxToFlex && !!lbToFlexCount && 'text-primary')}
            />
            {lbToFlexCount}
          </div>
          <div class="col-span-full text-center flex flex-col">
            <span class="font-medium">
              <Icon icon="i-mdi-cloud-outline" />
              {$t`${serverName} - FieldWorks`}
            </span>
            <span class="text-foreground/80">
              {$t`Last change: ${formatDate(lastFlexSyncDate, undefined,
                remoteStatus.status === ProjectSyncStatusEnum.NeverSynced ? $t`Never` : $t`Unknown`)}`}
            </span>
            {#if remoteStatus.status === ProjectSyncStatusEnum.Unknown}
              {#if remoteStatus.errorCode === ProjectSyncStatusErrorCode.NotLoggedIn}
                {$t`Not logged in`}
              {:else}
                <span class="text-destructive brightness-200">
                  {$t`Error: ${remoteStatus.errorMessage ?? $t`Unknown`}`}
                </span>
              {/if}
            {/if}
          </div>
        {/if}
      </div>
    {/if}
  </DialogContent>
</Dialog>
