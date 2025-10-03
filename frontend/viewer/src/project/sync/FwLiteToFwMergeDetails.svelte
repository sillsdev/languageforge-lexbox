<script lang="ts">
  import {ProjectSyncStatusEnum} from '$lib/dotnet-types/generated-types/LexCore/Sync/ProjectSyncStatusEnum';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import type {IProjectSyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import {t, T} from 'svelte-i18n-lingui';
  import SyncArrow from './SyncArrow.svelte';
  import flexLogo from '$lib/assets/flex-logo.png';
  import logoLight from '$lib/assets/logo-light.svg';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import type {ILexboxServer} from '$lib/dotnet-types';

  let {
    syncStatus,
    remoteStatus,
    server,
    loadingSyncLexboxToFlex = $bindable(false),
    loadingSyncLexboxToLocal,
    canSyncLexboxToFlex,
    syncLexboxToFlex = async () => {
    },
    onLoginStatusChange = () => {
    },
  }: {
    syncStatus: SyncStatus;
    remoteStatus?: IProjectSyncStatus,
    server?: ILexboxServer;
    loadingSyncLexboxToFlex: boolean,
    loadingSyncLexboxToLocal: boolean,
    canSyncLexboxToFlex?: boolean,
    syncLexboxToFlex?: () => Promise<void>
      onLoginStatusChange?: (status: 'logged-in' | 'logged-out') => void;
  } = $props();
  const lastFlexSyncDate = $derived(remoteStatus?.lastMercurialCommitDate ? new Date(remoteStatus.lastMercurialCommitDate) : undefined);
  const lastFwLiteSyncDate = $derived(remoteStatus?.lastCrdtCommitDate ? new Date(remoteStatus.lastCrdtCommitDate) : undefined);
  let lexboxToFlexCount = $derived(remoteStatus?.pendingCrdtChanges ?? '?');
  let flexToLexboxCount = $derived(remoteStatus?.pendingMercurialChanges ?? '?');

  function onSyncLexboxToFlex() {
    loadingSyncLexboxToFlex = true;
    void syncLexboxToFlex().finally(() => {
      loadingSyncLexboxToFlex = false;
    });
  }
</script>

<div class="grid grid-cols-[1fr_auto_1fr] gap-4">
  <div class="border rounded flex flex-col items-center justify-center text-center p-2">
    <Icon src={logoLight} class="size-10 mb-1" alt={$t`FieldWorks logo`}/>
    <p>FieldWorks Lite</p>
    <span class="text-sm text-foreground/80">
      <T msg="Last change: #">
        <FormatRelativeDate
          date={lastFwLiteSyncDate}
          showActualDate
          defaultValue={remoteStatus?.status === ProjectSyncStatusEnum.NeverSynced ? $t`Never` : $t`Unknown`}/>
      </T>
    </span>
  </div>
  <div class="flex flex-col items-center justify-center gap-1">
      {#if syncStatus === SyncStatus.Offline}
        <Icon icon="i-mdi-cloud-off-outline"  class="size-10 m-4" />
      {:else if syncStatus === SyncStatus.NotLoggedIn && server}
        <LoginButton
          text={$t`Login`}
          status={{loggedIn: false, server: server}}
          statusChange={s => onLoginStatusChange(s)}/>
      {:else}
        <span>{$t`${flexToLexboxCount} Commits`}</span>
        <SyncArrow dir="left" tailLength={120} size={1.25}/>
        <Button
          loading={loadingSyncLexboxToFlex}
          disabled={loadingSyncLexboxToLocal || !canSyncLexboxToFlex || !remoteStatus}
          onclick={onSyncLexboxToFlex}
          icon="i-mdi-sync"
          iconProps={{ class: 'size-5' }}>
          {$t`Sync`}
        </Button>
        <SyncArrow dir="right" tailLength={120} size={1.25}/>
        <span>{$t`${lexboxToFlexCount} Changes`}</span>
      {/if}
  </div>
  <div class="border rounded flex flex-col items-center justify-center text-center p-2">
    <Icon src={flexLogo} class="size-10 mb-1" alt={$t`FieldWorks logo`}/>
    <p>FieldWorks Classic</p>
    <span class="text-sm text-foreground/80">
      <T msg="Last change: #">
        <FormatRelativeDate
          date={lastFlexSyncDate}
          showActualDate
          defaultValue={remoteStatus?.status === ProjectSyncStatusEnum.NeverSynced ? $t`Never` : $t`Unknown`}/>
      </T>
    </span>
  </div>
</div>
