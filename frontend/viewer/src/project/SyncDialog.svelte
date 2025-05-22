<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import { Icon } from '$lib/components/ui/icon';
  import type { IProjectSyncStatus } from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import { Dialog, DialogContent, DialogHeader, DialogTitle } from '$lib/components/ui/dialog';
  import { t, plural } from 'svelte-i18n-lingui';
  import { AppNotification } from '$lib/notifications/notifications';
  import { QueryParamStateBool } from '$lib/utils/url.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import { useSyncStatusService } from '$lib/services/sync-status-service';
  import { FormatDate } from '$lib/components/ui/format-date';

  // Get status in calling code by something like the following:
  const service = useSyncStatusService();
  let status: IProjectSyncStatus | undefined = $state();
  let loading = $state(false);
  const openQueryParam = new QueryParamStateBool(
    { key: 'syncDialogOpen', replaceOnDefaultValue: true, allowBack: true },
    false,
  );

  let { syncLbToLocal } = $props<{
    syncLbToLocal: () => void | Promise<void>;
  }>();

  const localToLbCount = 0; // TODO: track this at some point
  const lbToLocalCount = 0; // TODO: track this at some point
  const lastLocalSyncDate = $derived(new Date(status?.lastCrdtCommitDate ?? ''));
  const lastFlexSyncDate = $derived(new Date(status?.lastMercurialCommitDate ?? ''));
  let lbToFlexCount = $derived(status?.pendingCrdtChanges ?? 0);
  let flexToLbCount = $derived(status?.pendingMercurialChanges ?? 0);

  export function open(): void {
    loading = true;
    let promise = service.getStatus();
    if (!promise) {
      // Can only happen if the sync status service was unavailable
      status = undefined;
      loading = false;
    } else {
      void promise.then((result) => {
        status = result;
        loading = false;
      });
    }
    openQueryParam.current = true;
  }

  let loadingSyncLbToFlex = $state(false);
  async function syncLbToFlex() {
    loadingSyncLbToFlex = true;
    let result = await service.triggerFwHeadlessSync();
    loadingSyncLbToFlex = false;
    if (result) {
      const fwdataChangesText = $plural(result.fwdataChanges, { one: '# change', other: '# changes' });
      const crdtChangesText = $plural(result.crdtChanges, { one: '# change', other: '# changes' });
      AppNotification.display(
        $t`${fwdataChangesText} synced to FieldWorks. ${crdtChangesText} synced to FieldWorks Lite.`,
        'success',
      );
      // Optimisticlly update status, then query it
      lbToFlexCount = 0;
      flexToLbCount = 0;
      const promise = service.getStatus();
      if (promise) {
        status = await promise;
      }
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
    {:else if !status}
      <div>{$t`Error getting sync status. Are you logged in to the LexBox server?`}</div>
    {:else}
      <div class="grid grid-rows-5 grid-cols-[auto_auto_auto] grid-template-columns-auto justify-around gap-y-4 gap-x-8">
        <div class="col-span-3 text-center content-center flex flex-col items-center">
          <Icon icon="i-mdi-monitor-cellphone" class="size-10" />
        </div>
        <div class="text-right content-center">{lbToLocalCount}<Icon icon="i-mdi-arrow-up" /></div>
        <div class="content-center text-center">
          <Button onclick={syncLbToLocal} icon="i-mdi-sync" iconProps={{ class: 'size-5' }}>{$t`Synchronize`}</Button>
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
          <Button loading={loadingSyncLbToFlex} onclick={syncLbToFlex} icon="i-mdi-sync" iconProps={{ class: 'size-5' }}
            >{$t`Synchronize`}</Button
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
