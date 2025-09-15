<script lang="ts">
  import * as Tabs from '$lib/components/ui/tabs';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import {Icon} from '$lib/components/ui/icon';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import {Button} from '$lib/components/ui/button';
  import type {IProjectSyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/IProjectSyncStatus';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';
  import type {ILexboxServer} from '$lib/dotnet-types';
  import {fade} from 'svelte/transition';
  import {t, T} from 'svelte-i18n-lingui';
  import SyncArrow from './SyncArrow.svelte';
  import FwLiteToFwMergeDetails from './FwLiteToFwMergeDetails.svelte';

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
  const serverName = $derived(server?.displayName ?? serverId ?? 'unknown');
  const serverProjectUrl = $derived(`${server?.authority}/project/${encodeURIComponent(projectCode ?? '')}`);
  const isOffline = $derived(syncStatus === SyncStatus.Offline);
  const showRemote = $derived(!!server);

  let loadingSyncLexboxToFlex = $state(false);

  let loadingSyncLexboxToLocal = $state(false);

  function onSyncLexboxToLocal() {
    loadingSyncLexboxToLocal = true;
    void syncLexboxToLocal().finally(() => {
      loadingSyncLexboxToLocal = false;
    });
  }
</script>
<Tabs.Root value="lite">
  {#if showRemote}
    <Tabs.List class="w-full mb-2">
      <Tabs.Trigger class="flex-1" value="lite">{$t`FieldWorks Lite`}</Tabs.Trigger>
      <Tabs.Trigger class="flex-1" value="classic">{$t`FieldWorks Classic`}</Tabs.Trigger>
    </Tabs.List>
  {/if}
  <Tabs.Content value="lite">
    <div in:fade class="grid grid-rows-[auto] grid-cols-[1fr_auto_1fr] gap-y-4 gap-x-8">

      <!-- Status local to remote -->
      <div class="col-span-full text-center border rounded py-2">
        <a class="inline-flex flex-col items-center" href={serverProjectUrl} target="_blank">
          <Icon icon={!isOffline ? 'i-mdi-cloud-outline' : 'i-mdi-cloud-off-outline'}  class="size-10" />
          <span class="underline">{serverName}</span>
        </a>
      </div>
      <!--  arrows and sync counts -->
      <div class="col-span-full text-center grid justify-center items-center"
           style="grid-template-columns: 1fr auto 1fr">
        <div class="px-4 max-w-56">
          <span class="text-foreground/80">
            <T msg="Last sync: #">
              {#if !lastLocalSyncDate}
                <span>{$t`Never`}</span>
              {:else}
                <FormatRelativeDate date={lastLocalSyncDate} showActualDate />
              {/if}
            </T>
          </span>
        </div>
        <div class="grid justify-center items-center min-h-12" style="grid-template-columns: 1fr auto auto auto 1fr">
          <span class="text-end">{remoteToLocalCount ?? '?'}</span>
          <SyncArrow dir="down" tailLength={40} size={2} class="translate-y-[1px]"/>
          {#if remoteToLocalCount === 0 && localToRemoteCount === 0}
            <span>{$t`Up to date`}</span>
          {:else}
            <span>{$t`Pending`}</span>
          {/if}
          <SyncArrow dir="up" tailLength={40} size={2} class="translate-y-[-1px]"/>
          <span class="text-start">{localToRemoteCount ?? '?'}</span>
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
            <!--  nothing to show -->
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

      <!--  local box-->
      <div class="text-center col-span-full border rounded py-2">
        <Icon icon="i-mdi-cellphone" class="!size-10 md:i-mdi-monitor"/>
        <p>{$t`Local`}</p>
      </div>
    </div>
  </Tabs.Content>
  {#if showRemote}
    <Tabs.Content value="classic">
      <FwLiteToFwMergeDetails
        {syncStatus}
        {server}
        {onLoginStatusChange}
        {remoteStatus}
        {syncLexboxToFlex}
        bind:loadingSyncLexboxToFlex
        {loadingSyncLexboxToLocal}
        {canSyncLexboxToFlex}/>
    </Tabs.Content>
  {/if}
</Tabs.Root>
