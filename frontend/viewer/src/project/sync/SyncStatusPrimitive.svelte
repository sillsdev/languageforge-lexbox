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
  import SyncArrow from './SyncArrow.svelte';

  interface Props {
    syncStatus: SyncStatus;
    remoteStatus?: IProjectSyncStatus;
    localStatus?: IPendingCommits;
    server?: ILexboxServer;
    serverId?: string;
    projectCode?: string;
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
    projectCode,
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
  const isOffline = $derived(syncStatus === SyncStatus.Offline);

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
<div in:fade class="grid grid-rows-[auto] grid-cols-[1fr_auto_1fr] gap-y-4 gap-x-8">
  {#if false && server && syncStatus === SyncStatus.Success}
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
        <div class="animate-pulse inline-block h-4 align-baseline dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-4"></div>
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
        <div class="animate-pulse inline-block h-4 align-baseline dark:bg-neutral-50/50 bg-neutral-500 rounded-full w-4"></div>
      {:else}
        {lexboxToFlexCount}
      {/if}
    </div>
  {/if}

  <!-- Status local to remote -->
  <div class="col-span-full text-center flex flex-col border rounded py-2">
    <a href={server?.authority + '/project/' + projectCode} target="_blank">
      <Icon icon={!isOffline ? 'i-mdi-cloud-outline' : 'i-mdi-cloud-off-outline'}/>
      <span class="underline">{serverName}</span>
      {#if isOffline}
        <span>(Offline)</span>
      {/if}
    </a>
    <span class="text-foreground/80">
      {$t`Last change: ${formatDate(lastLocalSyncDate)}`}
    </span>
  </div>
  <div class="col-span-full text-center grid justify-center items-center" style="grid-template-columns: 1fr auto 1fr">
    <div>
<!--      blank spacer-->
    </div>
    <div class="grid justify-center items-center min-h-12" style="grid-template-columns: 1fr auto auto auto 1fr">
      <span class="text-end">{remoteToLocalCount ?? '?'}</span>
      <SyncArrow dir="down" length="4em" class="translate-y-[-1px]"/>
      {#if remoteToLocalCount === 0 && localToRemoteCount === 0}
        <span>Up to date</span>
      {:else}
        <span>Pending</span>
      {/if}
      <SyncArrow dir="up" length="4em" class="translate-y-[1px]"/>
      <span class="text-start">{localToRemoteCount}</span>
    </div>
    <div class="content-center pl-2">
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
            {$t`Syncing...`}
          {:else}
            {$t`Auto sync`}
          {/if}
        </Button>
      {:else if syncStatus === SyncStatus.Offline}
      {:else if syncStatus === SyncStatus.NotLoggedIn && server}
        <LoginButton
          text={$t`Login`}
          status={{loggedIn: false, server: server}}
          statusChange={s => onLoginStatusChange(s)}/>
      {:else if syncStatus === SyncStatus.NoServer || !server}
        <!-- nothing to show -->
      {:else}
        <div class="text-destructive">{$t`Error getting sync status.`}</div>
      {/if}
    </div>
  </div>

  <div class="text-center col-span-full border rounded py-2">
    <Icon icon="i-mdi-monitor-cellphone" class="size-10"/>
    <p>Local</p>
  </div>
</div>
