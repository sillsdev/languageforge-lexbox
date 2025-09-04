<script lang="ts">
  import {formatDate} from '$lib/components/ui/format';
  import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
  import {ProjectSyncStatusErrorCode} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusErrorCode';
  import {cn} from '$lib/utils';
  import {Button} from '$lib/components/ui/button';
  import {Icon, PingingIcon} from '$lib/components/ui/icon';
  import type {IProjectSyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import {t} from 'svelte-i18n-lingui';

  let {
    remoteStatus,
    serverName,
    loadingSyncLexboxToFlex = $bindable(false),
    loadingSyncLexboxToLocal,
    canSyncLexboxToFlex,
    syncLexboxToFlex = async () => {
    },
  }: {
    remoteStatus?: IProjectSyncStatus,
    serverName: string,
    loadingSyncLexboxToFlex: boolean,
    loadingSyncLexboxToLocal: boolean,
    canSyncLexboxToFlex?: boolean,
    syncLexboxToFlex?: () => Promise<void>
  } = $props();
  const lastFlexSyncDate = $derived(remoteStatus?.lastMercurialCommitDate ? new Date(remoteStatus.lastMercurialCommitDate) : undefined);
  let lexboxToFlexCount = $derived(remoteStatus?.pendingCrdtChanges);
  let flexToLexboxCount = $derived(remoteStatus?.pendingMercurialChanges);

  function onSyncLexboxToFlex() {
    loadingSyncLexboxToFlex = true;
    void syncLexboxToFlex().finally(() => {
      loadingSyncLexboxToFlex = false;
    });
  }
</script>
<details>
  <summary>
    Update FieldWorks
  </summary>
  <div class="col-span-full text-center flex flex-col">
      <span class="font-medium">
        <Icon icon="i-mdi-cloud-outline"/>
        {$t`${serverName} - FieldWorks`}
      </span>
    <span class="text-foreground/80">
        {#if !remoteStatus}
          <span class="animate-pulse inline-block h-4 dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-64"></span>
        {:else}
          {$t`Last change: ${formatDate(lastFlexSyncDate, undefined, remoteStatus.status === ProjectSyncStatusEnum.NeverSynced ? $t`Never` : $t`Unknown`)}`}
        {/if}
      </span>
    {#if remoteStatus?.status === ProjectSyncStatusEnum.Unknown}
      {#if remoteStatus.errorCode === ProjectSyncStatusErrorCode.NotLoggedIn}
        {$t`Not logged in`}
      {:else}
          <span class="text-destructive brightness-200">
            {$t`Error: ${remoteStatus.errorMessage ?? $t`Unknown`}`}
          </span>
      {/if}
    {/if}
  </div>

  <div class="text-center content-center">
    {#if !remoteStatus}
      <div
        class="animate-pulse inline-block h-4 align-baseline dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-4"></div>
    {:else}
      {flexToLexboxCount}
    {/if}
    <PingingIcon
      icon="i-mdi-arrow-down"
      ping={loadingSyncLexboxToFlex && !!flexToLexboxCount}
      class={cn(loadingSyncLexboxToFlex && !!flexToLexboxCount && 'text-primary')}
    />
  </div>
  <div class="content-center text-center">
    <Button
      loading={loadingSyncLexboxToFlex}
      disabled={loadingSyncLexboxToLocal || !canSyncLexboxToFlex || !remoteStatus}
      onclick={onSyncLexboxToFlex}
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
      icon="i-mdi-arrow-up"
      ping={loadingSyncLexboxToFlex && !!lexboxToFlexCount}
      class={cn(loadingSyncLexboxToFlex && !!lexboxToFlexCount && 'text-primary')}
    />
    {#if !remoteStatus}
      <div
        class="animate-pulse inline-block h-4 align-baseline dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-4"></div>
    {:else}
      {lexboxToFlexCount}
    {/if}
  </div>

</details>
