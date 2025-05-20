<script lang="ts">
  import { Button } from '$lib/components/ui/button';
  import { Icon } from '$lib/components/ui/icon';
  import { Duration, DurationUnits } from 'svelte-ux';
  import type { IProjectSyncStatus } from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import { Dialog, DialogContent, DialogHeader, DialogTitle } from '$lib/components/ui/dialog';
  import { t } from 'svelte-i18n-lingui';
  import { QueryParamStateBool } from '$lib/utils/url.svelte';
  import Loading from '$lib/components/Loading.svelte';
  import { useSyncStatusService } from '$lib/services/sync-status-service';

  // Get status in calling code by something like the following:
  const service = useSyncStatusService();
  let status: IProjectSyncStatus | undefined = $state();
  let loading = $state(false);
  const openQueryParam = new QueryParamStateBool(
    { key: 'syncDialogOpen', replaceOnDefaultValue: true, allowBack: true },
    false,
  );

  export function open(): void {
    // status = await service.getStatus('How do I find the project ID to use in the fallback?'); // TODO
    loading = true;
    service.getStatus().then((result) => {
      status = result;
      loading = false;
    });
    openQueryParam.current = true;
  }

  let { syncLbToLocal, syncLbToFlex } = $props<{
    syncLbToLocal: () => void; // Or perhaps Promise<void>
    syncLbToFlex: () => void; // Or perhaps Promise<void>
  }>();

  const localToLbCount = 0; // TODO: track this at some point
  const lbToLocalCount = 0; // TODO: track this at some point
  const lastLocalSyncDate = $derived(new Date(status?.lastCrdtCommitDate ?? ''));
  const lastFlexSyncDate = $derived(new Date(status?.lastMercurialCommitDate ?? ''));
  // TODO: Make sure these are assigned to the right slots
  const lbToFlexCount = $derived(status?.pendingMercurialChanges ?? 0);
  const flexToLbCount = $derived(status?.pendingCrdtChanges ?? 0);
</script>

<Dialog bind:open={openQueryParam.current}>
  <DialogContent class="sm:min-h-fit">
    <DialogHeader>
      <DialogTitle>{$t`Sync`}</DialogTitle>
    </DialogHeader>
    {#if loading}
      <!-- TODO: Show loading as an overlay? -->
      <Loading />
    {:else}
      <div class="grid grid-rows-5 grid-cols-3 justify-around rounded-lg border-4">
        <div class="col-span-3 text-center">{$t`My computer`}</div>
        <div class="ml-2">{lbToLocalCount}<Icon icon="i-mdi-arrow-up" /></div>
        <div class="text-center"><Button onclick={syncLbToLocal}><Icon icon="i-mdi-recycle" /></Button></div>
        <div class="mr-2 text-right"><Icon icon="i-mdi-arrow-down" />{localToLbCount}</div>
        <div class="col-span-3 text-center">
          {$t`LexBox`}<br />{$t`Last change: `}<Duration
            totalUnits={2}
            start={lastLocalSyncDate}
            minUnits={DurationUnits.Second}
          />{$t` ago`}
        </div>
        <div class="ml-2">{flexToLbCount}<Icon icon="i-mdi-arrow-up" /></div>
        <div class="text-center"><Button onclick={syncLbToFlex}><Icon icon="i-mdi-recycle" /></Button></div>
        <div class="mr-2 text-right"><Icon icon="i-mdi-arrow-down" />{lbToFlexCount}</div>
        <div class="col-span-3 text-center">
          {$t`FieldWorks`}<br />{$t`Last change: `}<Duration
            totalUnits={2}
            start={lastFlexSyncDate}
            minUnits={DurationUnits.Second}
          />{$t` ago`}
        </div>
      </div>
    {/if}
  </DialogContent>
</Dialog>
