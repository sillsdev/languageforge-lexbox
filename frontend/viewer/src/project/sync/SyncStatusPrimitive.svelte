<script lang="ts">
  import {cn} from '$lib/utils';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {formatDate} from '$lib/components/ui/format';
  import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
  import {ProjectSyncStatusErrorCode} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusErrorCode';
  import {Icon, PingingIcon} from '$lib/components/ui/icon';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import {Button} from '$lib/components/ui/button';
  import type {IProjectSyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';
  import type {ILexboxServer} from '$lib/dotnet-types';
  import {fade} from 'svelte/transition';
  import {t} from 'svelte-i18n-lingui';

  interface Props {
    syncStatus: SyncStatus;
    remoteStatus?: IProjectSyncStatus;
    localStatus?: IPendingCommits;
    server?: ILexboxServer;
    serverId?: string;
    latestCommitDate?: string;
    canSyncLexboxToFlex?: boolean;
    syncLexboxToFlex?: () => Promise<void>;
    syncLexboxToLocal?: () => Promise<void>;
    onLoginStatusChange?: (status: 'logged-in' | 'logged-out') => void;
  }

  const {
    syncStatus,
    remoteStatus,
    localStatus,
    server,
    serverId,
    latestCommitDate,
    canSyncLexboxToFlex,
    syncLexboxToFlex = async () => {
    },
    syncLexboxToLocal = async () => {
    },
    onLoginStatusChange = () => {
    },
  }: Props = $props();


  let remoteToLocalCount = $derived(localStatus?.remote);
  let localToRemoteCount = $derived(localStatus?.local);
  let lastLocalSyncDate = $derived(latestCommitDate ? new Date(latestCommitDate) : undefined);
  const lastFlexSyncDate = $derived(remoteStatus?.lastMercurialCommitDate ? new Date(remoteStatus.lastMercurialCommitDate) : undefined);
  let lexboxToFlexCount = $derived(remoteStatus?.pendingCrdtChanges);
  let flexToLexboxCount = $derived(remoteStatus?.pendingMercurialChanges);
  const serverName = $derived(server?.displayName ?? serverId ?? 'unknown');

  let loadingSyncLexboxToFlex = $state(false);
  function onSyncLexboxToFlex() {
    loadingSyncLexboxToFlex = true;
    void syncLexboxToFlex().finally(() => {
      loadingSyncLexboxToFlex = false;
    });
  }
  let loadingSyncLexboxToLocal = $state(false);
  function onSyncLexboxToLocal() {
    loadingSyncLexboxToLocal = true;
    void syncLexboxToLocal().finally(() => {
      loadingSyncLexboxToLocal = false;
    });
  }
</script>
<!-- 1fr_7fr_1fr seems to be a reliable way to prevent the buttons states from resizing the dialog -->
<div in:fade
     class="grid grid-rows-[auto] grid-cols-[1fr_7fr_1fr] gap-y-6 gap-x-8">
  <div class="col-span-full text-center">
    <Icon icon="i-mdi-monitor-cellphone" class="size-10"/>
  </div>
  <div class="text-center content-center">
    {remoteToLocalCount ?? '?'}
    <PingingIcon
      icon="i-mdi-arrow-up"
      ping={loadingSyncLexboxToLocal && !!remoteToLocalCount}
      class={cn(loadingSyncLexboxToLocal && !!remoteToLocalCount && 'text-primary')}
    />
  </div>
  <div class="content-center text-center">
    {#if syncStatus === SyncStatus.Success}
      <Button
        variant="outline"
        class="border-primary text-primary hover:text-primary"
        loading={loadingSyncLexboxToLocal}
        disabled={loadingSyncLexboxToFlex}
        onclick={onSyncLexboxToLocal}
        icon="i-mdi-sync"
        iconProps={{ class: 'size-5' }}>
        {#if loadingSyncLexboxToLocal}
          {$t`Synchronizing...`}
        {:else}
          {$t`Auto synchronizing`}
        {/if}
      </Button>
    {:else if syncStatus === SyncStatus.Offline}
      <div>
        <Icon icon="i-mdi-cloud-off-outline"/> {$t`Offline`}</div>
    {:else if syncStatus === SyncStatus.NotLoggedIn && server}
      <LoginButton
        text={$t`Login`}
        status={{loggedIn: false, server: server}}
        statusChange={s => onLoginStatusChange(s)}/>
    {:else if syncStatus === SyncStatus.NoServer && serverId}
      <div>{$t`Unknown server: ${serverId}`}</div>
    {:else if syncStatus === SyncStatus.NoServer}
      <div>{$t`No server configured`}</div>
    {:else}
      <div class="text-destructive">{$t`Error getting sync status.`}</div>
    {/if}
  </div>
  <div class="text-center content-center">
    <PingingIcon
      icon="i-mdi-arrow-down"
      ping={loadingSyncLexboxToLocal && !!localToRemoteCount}
      class={cn(loadingSyncLexboxToLocal && !!localToRemoteCount && 'text-primary')}
    />
    {localToRemoteCount}
  </div>
  <div class="col-span-full text-center flex flex-col">
          <span class="font-medium">
            <Icon icon="i-mdi-cloud-outline"/>
            {$t`${serverName} - FieldWorks Lite`}
          </span>
    <span class="text-foreground/80">
            {$t`Last change: ${formatDate(lastLocalSyncDate)}`}
          </span>
  </div>
  {#if server && syncStatus === SyncStatus.Success}
    <div class="text-center content-center">
      {#if !remoteStatus}
        <div class="animate-pulse inline-block h-4 align-baseline dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-4"></div>
      {:else}
        {flexToLexboxCount}
      {/if}
      <PingingIcon
        icon="i-mdi-arrow-up"
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
        icon="i-mdi-arrow-down"
        ping={loadingSyncLexboxToFlex && !!lexboxToFlexCount}
        class={cn(loadingSyncLexboxToFlex && !!lexboxToFlexCount && 'text-primary')}
      />
      {#if !remoteStatus}
        <div class="animate-pulse inline-block h-4 align-baseline dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-4"></div>
      {:else}
        {lexboxToFlexCount}
      {/if}
    </div>
    <div class="col-span-full text-center flex flex-col">
      <span class="font-medium">
        <Icon icon="i-mdi-cloud-outline"/>
        {$t`${serverName} - FieldWorks`}
      </span>
      <span class="text-foreground/80">
        {#if !remoteStatus}
          <div class="animate-pulse inline-block h-4 dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-64"></div>
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
  {/if}
</div>
