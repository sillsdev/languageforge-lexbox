<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import { Icon } from '$lib/components/ui/icon';
  import type { IProjectSyncStatus } from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import type { ISyncResult } from '$lib/dotnet-types/generated-types/LexCore/Sync/ISyncResult';
  import { Dialog, DialogContent, DialogHeader, DialogTitle } from '$lib/components/ui/dialog';
  import { t, plural } from 'svelte-i18n-lingui';
  import { AppNotification } from '$lib/notifications/notifications';
  import { QueryParamStateBool } from '$lib/utils/url.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import { useSyncStatusService } from '$lib/services/sync-status-service';
  import { FormatDate } from '$lib/components/ui/format-date';

  // Get status in calling code by something like the following:
  const service = useSyncStatusService();
  let remoteStatus: IProjectSyncStatus | undefined = $state();
  let localStatus: ISyncResult | undefined = $state();
  let loading = $state(false);
  const openQueryParam = new QueryParamStateBool(
    { key: 'syncDialogOpen', replaceOnDefaultValue: true, allowBack: true },
    false,
  );

  let lbToLocalCount = $derived(localStatus?.fwdataChanges ?? 0);
  let localToLbCount = $derived(localStatus?.crdtChanges ?? 0);
  const lastLocalSyncDate = $derived(new Date(remoteStatus?.lastCrdtCommitDate ?? ''));
  const lastFlexSyncDate = $derived(new Date(remoteStatus?.lastMercurialCommitDate ?? ''));
  let lbToFlexCount = $derived(remoteStatus?.pendingCrdtChanges ?? 0);
  let flexToLbCount = $derived(remoteStatus?.pendingMercurialChanges ?? 0);

  export function open(): void {
    loading = true;
    let remotePromise = service.getStatus();
    let localPromise = service.getLocalStatus();
    if (!remotePromise || !localPromise) {
      // Can only happen if the sync status service was unavailable
      localStatus = undefined;
      remoteStatus = undefined;
      loading = false;
    } else {
      void Promise.all([localPromise, remotePromise]).then(([localResult, remoteResult]) => {
        localStatus = localResult;
        remoteStatus = remoteResult;
        loading = false;
      });
    }
    openQueryParam.current = true;
  }

  let loadingSyncLexboxToFlex = $state(false);
  async function syncLexboxToFlex() {
    loadingSyncLexboxToFlex = true;
    let result = await service.triggerFwHeadlessSync();
    loadingSyncLexboxToFlex = false;
    if (result) {
      const fwdataChangesText = $plural(result.fwdataChanges, { one: '# change', other: '# changes' });
      const crdtChangesText = $plural(result.crdtChanges, { one: '# change', other: '# changes' });
      AppNotification.display(
        $t`${fwdataChangesText} synced to FieldWorks. ${crdtChangesText} synced to FieldWorks Lite.`,
        'success',
      );
      // Optimistically update status, then query it
      lbToFlexCount = 0;
      flexToLbCount = 0;
      const promise = service.getStatus();
      if (promise) {
        remoteStatus = await promise;
      }
    }
  }

  let loadingSyncLexboxToLocal = $state(false);
  async function syncLexboxToLocal() {
    loadingSyncLexboxToFlex = true;
    console.log('TODO: Implement forcing local sync');
    loadingSyncLexboxToFlex = false;
    // Optimistically update status, then query it
    lbToLocalCount = 0;
    localToLbCount = 0;
    const promise = service.getLocalStatus();
    if (promise) {
      localStatus = await promise;
    }
  }
</script>

<Dialog bind:open={openQueryParam.current}>
  <DialogContent class="sm:min-h-fit sm:min-w-fit">
    <DialogHeader>
      <DialogTitle>{$t`Synchronize`}</DialogTitle>
    </DialogHeader>
    {#if loading}
      <Loading />
    {:else if !remoteStatus}
      <div>{$t`Error getting sync status. Are you logged in to the LexBox server?`}</div>
    {:else}
      <div
        class="grid grid-rows-5 grid-cols-[auto_auto_auto] grid-template-columns-auto justify-around gap-y-4 gap-x-8"
      >
        <div class="col-span-3 text-center content-center flex flex-col items-center">
          <Icon icon="i-mdi-monitor-cellphone" class="size-10" />
        </div>
        <div class="text-right content-center">{lbToLocalCount}<Icon icon="i-mdi-arrow-up" /></div>
        <div class="content-center text-center">
          <Button
            loading={loadingSyncLexboxToLocal}
            onclick={syncLexboxToLocal}
            icon="i-mdi-sync"
            iconProps={{ class: 'size-5' }}>{$t`Synchronize`}</Button
          >
        </div>
        <div class="text-left content-center"><Icon icon="i-mdi-arrow-down" />{localToLbCount}</div>
        <div class="col-span-3 text-center flex flex-col">
          <span class="font-medium">
            <Icon icon="i-mdi-cloud-outline" />
            Lexbox - FieldWorks Lite
          </span>
          <span class="text-foreground/80">
            {$t`Last change: `}
            <FormatDate date={lastLocalSyncDate} />
          </span>
        </div>
        <div class="text-right content-center">{flexToLbCount}<Icon icon="i-mdi-arrow-up" /></div>
        <div class="content-center text-center">
          <Button
            loading={loadingSyncLexboxToFlex}
            onclick={syncLexboxToFlex}
            icon="i-mdi-sync"
            iconProps={{ class: 'size-5' }}>{$t`Synchronize`}</Button
          >
        </div>
        <div class="text-left content-center"><Icon icon="i-mdi-arrow-down" />{lbToFlexCount}</div>
        <div class="col-span-3 text-center flex flex-col">
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
