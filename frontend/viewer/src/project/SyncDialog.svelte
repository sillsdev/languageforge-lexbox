<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import { Icon, PingingIcon } from '$lib/components/ui/icon';
  import type { IProjectSyncStatus } from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import type { ISyncResult } from '$lib/dotnet-types/generated-types/LexCore/Sync/ISyncResult';
  import { Dialog, DialogContent, DialogHeader, DialogTitle } from '$lib/components/ui/dialog';
  import { t, plural } from 'svelte-i18n-lingui';
  import { AppNotification } from '$lib/notifications/notifications';
  import { QueryParamStateBool } from '$lib/utils/url.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import { useSyncStatusService } from '$lib/services/sync-status-service';
  import { FormatDate } from '$lib/components/ui/format-date';
  import { useMiniLcmApi } from '$lib/services/service-provider';
  import {watch} from 'runed';
  import {fade} from 'svelte/transition';
  import {delay} from '$lib/utils/time';
  import {cn} from '$lib/utils';

  // Get status in calling code by something like the following:
  const service = useSyncStatusService();
  const api = useMiniLcmApi();
  let remoteStatus: IProjectSyncStatus | undefined = $state();
  let localStatus: ISyncResult | undefined = $state();
  let loading = $state(false);
  const openQueryParam = new QueryParamStateBool(
    { key: 'syncDialogOpen', replaceOnDefaultValue: true, allowBack: true },
    false,
  );

  let lbToLocalCount = $derived(localStatus?.fwdataChanges ?? 0);
  let localToLbCount = $derived(localStatus?.crdtChanges ?? 0);
  let latestCommitDate = $state<string | undefined>(undefined);
  let lastLocalSyncDate = $derived(latestCommitDate ? new Date(latestCommitDate) : undefined);
  const lastFlexSyncDate = $derived(remoteStatus?.lastMercurialCommitDate ? new Date(remoteStatus.lastMercurialCommitDate) : undefined);
  let lbToFlexCount = $derived(remoteStatus?.pendingCrdtChanges ?? 0);
  let flexToLbCount = $derived(remoteStatus?.pendingMercurialChanges ?? 0);

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
      let remotePromise = service.getStatus();
      let localPromise = service.getLocalStatus();
      let commitDatePromise = service.getLatestCommitDate();
      if (!remotePromise || !localPromise) {
        // Can only happen if the sync status service was unavailable
        localStatus = undefined;
        remoteStatus = undefined;
        latestCommitDate = '';
        loading = false;
      } else {
        [localStatus, remoteStatus, latestCommitDate] = await Promise.all([localPromise, remotePromise, commitDatePromise]);
      }
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
      const result = await service.triggerFwHeadlessSync();
      if (!result) {
        AppNotification.display($t`Failed to synchronize`, 'error');
        return;
      }
      const fwdataChangesText = $plural(result.fwdataChanges, { one: '# change', other: '# changes' });
      const crdtChangesText = $plural(result.crdtChanges, { one: '# change', other: '# changes' });
      AppNotification.display(
        $t`${fwdataChangesText} synced to FieldWorks. ${crdtChangesText} synced to FieldWorks Lite.`,
        'success',
      );
    } finally {
      loadingSyncLexboxToFlex = false;
    }

    // Optimistically update status, then query it
    lbToFlexCount = 0;
    flexToLbCount = 0;
    const statusPromise = service.getStatus();
    if (statusPromise) {
      // Auto-close dialog after successful FieldWorks sync
      [remoteStatus] = await Promise.all([statusPromise, delay(750)]);
      if (remoteStatus.pendingMercurialChanges === 0 && remoteStatus.pendingCrdtChanges === 0) {
        openQueryParam.current = false;
      }
    }
  }

  let loadingSyncLexboxToLocal = $state(false);
  async function syncLexboxToLocal() {
    if (api) {
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
        const datePromise = service.getLatestCommitDate();
        if (statusPromise && datePromise) {
          [localStatus, latestCommitDate] = await Promise.all([statusPromise, datePromise]);
        }
      } finally {
        loadingSyncLexboxToLocal = false;
      }
    }
  }
</script>

<Dialog bind:open={openQueryParam.current}>
  <DialogContent class="sm:min-h-80 sm:min-w-96 grid-rows-[auto_1fr] items-center">
    <DialogHeader>
      <DialogTitle>{$t`Synchronize`}</DialogTitle>
    </DialogHeader>
    {#if loading}
      <Loading class="place-self-center size-10" />
    {:else if !remoteStatus}
      <div>{$t`Error getting sync status. Are you logged in to the LexBox server?`}</div>
    {:else}
      <!-- 1fr_7fr_1fr seems to be a reliable way to prevent the buttons states from resizing the dialog -->
      <div in:fade
        class="grid grid-rows-5 grid-cols-[1fr_7fr_1fr] gap-y-4 gap-x-8"
      >
        <div class="col-span-full text-center">
          <Icon icon="i-mdi-monitor-cellphone" class="size-10" />
        </div>
        <div class="text-center content-center">
          {lbToLocalCount}
          <PingingIcon
            icon="i-mdi-arrow-up"
            ping={loadingSyncLexboxToLocal && !!lbToLocalCount}
            class={cn(loadingSyncLexboxToLocal && !!lbToLocalCount && 'text-primary')}
          />
        </div>
        <div class="content-center text-center">
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
            Lexbox - FieldWorks Lite
          </span>
          {#if lastLocalSyncDate}
            <span class="text-foreground/80">
              {$t`Last change: `}
              <FormatDate date={lastLocalSyncDate} />
            </span>
          {/if}
        </div>
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
            disabled={loadingSyncLexboxToLocal}
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
            Lexbox - FieldWorks
          </span>
          <span class="text-foreground/80">
            {$t`Last change: `}
            <FormatDate date={lastFlexSyncDate} />
          </span>
        </div>
      </div>
    {/if}
  </DialogContent>
</Dialog>
