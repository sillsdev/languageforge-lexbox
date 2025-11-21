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
  import {cn} from '$lib/utils';

  interface Props {
    syncStatus: SyncStatus;
    remoteStatus?: IProjectSyncStatus;
    localStatus?: IPendingCommits;
    server?: ILexboxServer;
    serverId?: string;
    projectCode?: string;
    latestSyncedCommitDate?: string;
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
    latestSyncedCommitDate,
    canSyncLexboxToFlex,
    syncLexboxToFlex = async () => {},
    syncLexboxToLocal = async () => {},
    onLoginStatusChange = () => {},
  }: Props = $props();


  let remoteToLocalCount = $derived(localStatus?.remote);
  let localToRemoteCount = $derived(localStatus?.local);
  let lastLocalSyncDate = $derived(latestSyncedCommitDate ? new Date(latestSyncedCommitDate) : undefined);
  const serverName = $derived(server?.displayName ?? serverId ?? 'unknown');
  const serverProjectUrl = $derived(`${server?.authority}/project/${encodeURIComponent(projectCode ?? '')}`);
  const isOffline = $derived(syncStatus === SyncStatus.Offline);
  const showRemote = $derived(!!server);
  const cloudIcon = $derived(isOffline ? 'i-mdi-cloud-off-outline' : 'i-mdi-cloud-outline');

  let loadingSyncLexboxToFlex = $state(false);

  let loadingSyncLexboxToLocal = $state(false);

  function onSyncLexboxToLocal() {
    loadingSyncLexboxToLocal = true;
    void syncLexboxToLocal().finally(() => {
      loadingSyncLexboxToLocal = false;
    });
  }
</script>
<Tabs.Root value="lite" class="flex md:flex-col flex-col-reverse">
  {#if showRemote}
    <Tabs.List class="w-full md:mb-2 max-md:mt-4 max-md:sticky bottom-0 z-[1]">
      <Tabs.Trigger class="flex-1" value="lite">{$t`FieldWorks Lite`}</Tabs.Trigger>
      <Tabs.Trigger class="flex-1 gap-2" value="classic"><Icon icon={cloudIcon} class="-my-1" /> {$t`Lexbox`}</Tabs.Trigger>
    </Tabs.List>
  {/if}
  <Tabs.Content value="lite">
    <p class="mb-6 text-lg text-center">
      {$t`Sync your changes with other FieldWorks Lite users`}
    </p>
    <div class="text-center my-2">
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
    <div in:fade class="grid grid-rows-[auto] grid-cols-[1fr_auto_1fr] gap-y-4 gap-x-8">
      <!-- Status local to remote -->
      <div class="col-span-full text-center border rounded pb-0.5">
        <Button class="flex-col h-auto gap-0 text-foreground hover:text-primary text-base" variant="link" href={serverProjectUrl} target="_blank" rel="noopener">
          <Icon icon={cloudIcon}  class="size-10 -mb-0.5" />
          <span class="underline">{serverName}</span>
        </Button>
      </div>
      <!--  arrows and sync counts -->
      <div class="col-span-full text-center grid justify-center items-center"
           style="grid-template-columns: 1fr auto 1fr">
        <div>
          <!-- blank spacer-->
        </div>
        <div class="grid justify-center items-center min-h-12 gap-2" style="grid-template-columns: 1fr auto auto auto 1fr">
          <span class="text-end" class:font-bold={Number(remoteToLocalCount)} class:text-primary={Number(remoteToLocalCount)}>{remoteToLocalCount ?? '?'}</span>
          <SyncArrow dir="down" tailLength={40}  size={1.5} class={cn('translate-y-[1px]', Number(remoteToLocalCount) && 'text-primary')} />
          <div class="flex flex-col gap-2 mx-2">
            {#if syncStatus === SyncStatus.Success}
              {#if remoteToLocalCount === 0 && localToRemoteCount === 0}
                <span>{$t`Up to date`}</span>
              {:else}
                <span>{$t`Pending`}</span>
              {/if}
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
                  {$t`Auto syncing`}
                {/if}
              </Button>
            {:else if syncStatus === SyncStatus.Offline}
              <span>{$t`Offline`}</span>
            {:else if syncStatus === SyncStatus.NotLoggedIn && server}
              <LoginButton
                text={$t`Login`}
                status={{loggedIn: false, server: server}}
                statusChange={s => onLoginStatusChange(s)}/>
            {:else if syncStatus === SyncStatus.NoServer || !server}
              {#if serverId || server}
                <span>{$t`Unknown server`}</span>
              {:else}
                <span>{$t`No server`}</span>
              {/if}
            {:else}
              <div class="text-destructive">{$t`Error getting sync status.`}</div>
            {/if}
            </div>
            <SyncArrow dir="up" tailLength={40} size={1.5} class={cn('translate-y-[-1px]', Number(localToRemoteCount) && 'text-primary')}/>
            <span class="text-start" class:font-bold={Number(localToRemoteCount)} class:text-primary={Number(localToRemoteCount)}>{localToRemoteCount ?? '?'}</span>
          </div>
          <div>
            <!-- blank spacer-->
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
