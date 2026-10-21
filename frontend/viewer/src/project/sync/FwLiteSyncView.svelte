<script lang="ts">
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import {Icon} from '$lib/components/ui/icon';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import {Button} from '$lib/components/ui/button';
  import type {IPendingCommits} from '$lib/dotnet-types/generated-types/FwLiteShared/Sync/IPendingCommits';
  import type {ILexboxServer} from '$lib/dotnet-types';
  import {fade} from 'svelte/transition';
  import {t, T} from 'svelte-i18n-lingui';
  import SyncArrow from './SyncArrow.svelte';
  import {cn} from '$lib/utils';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {useProjectEventBus} from '$lib/services/event-bus';
  import {CoalescedRefresh} from '$lib/utils/coalesced-refresh';
  import type {SyncTarget} from './sync-target';

  interface Props {
    syncStatus: SyncStatus;
    server?: ILexboxServer;
    serverId?: string;
    projectCode?: string;
    /** Which sync is running, shared with the other tab so only one runs at a time */
    syncing?: SyncTarget;
    onLoginStatusChange?: (status: 'logged-in' | 'logged-out') => void;
  }

  let {
    syncStatus,
    server,
    serverId,
    projectCode,
    syncing = $bindable(),
    onLoginStatusChange = () => {},
  }: Props = $props();

  const service = useSyncStatusService();
  const projectEventBus = useProjectEventBus();
  let localStatus = $state<IPendingCommits>();
  let latestSyncedCommitDate = $state<string>();
  const refresh = new CoalescedRefresh(async () => {
    await Promise.all([
      service.getLocalStatus().then(s => localStatus = s),
      service.getLatestSyncedCommitDate().then(s => latestSyncedCommitDate = s),
    ]);
  });

  // Any sync (this tab's or one in the background) changes the counts. forceFresh: a refresh already in
  // flight may have started before the sync. onSync replays the last event synchronously, so subscribing
  // first means the initial run() below joins that refresh instead of starting a second one.
  projectEventBus.onSync(e => {
    if (e.status === SyncStatus.Success) void refresh.run({forceFresh: true});
  });
  void refresh.run();

  let remoteToLocalCount = $derived(localStatus?.remote);
  let localToRemoteCount = $derived(localStatus?.local);
  let lastLocalSyncDate = $derived(latestSyncedCommitDate ? new Date(latestSyncedCommitDate) : undefined);
  const serverName = $derived(server?.displayName ?? serverId ?? 'unknown');
  const serverProjectUrl = $derived(`${server?.authority}/project/${encodeURIComponent(projectCode ?? '')}`);
  const cloudIcon = $derived(syncStatus === SyncStatus.Offline ? 'i-mdi-cloud-off-outline' : 'i-mdi-cloud-outline');

  async function sync() {
    const result = await service.triggerCrdtSync();
    if (!result.isSynced) {
      // Couldn't sync (offline, server unreachable, or not logged in): the backend has already published the
      // real sync status, which re-renders this view, so give gentle feedback instead of optimistically
      // zeroing the counts.
      AppNotification.display($t`Couldn't sync right now — your local changes are safe.`, {type: 'warning', timeout: 'short'});
      await refresh.run();
      return;
    }
    // Optimistically update status, then query it
    if (localStatus) {
      localStatus.remote = 0;
      localStatus.local = 0;
    }
    await refresh.run({forceFresh: true});
  }

  function onSyncClick() {
    syncing = 'lite';
    void sync().finally(() => {
      syncing = undefined;
    });
  }
</script>
<p class="mb-6 text-lg text-center">
  {$t`Sync your changes with other FieldWorks Lite users`}
</p>
<div class="text-center my-2">
  <span class="text-foreground/80">
    <T msg="Last sync: #">
      {#if !lastLocalSyncDate}
        <span>{$t`Never`}</span>
      {:else}
        <FormatRelativeDate date={lastLocalSyncDate} showActualDate live />
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
      <SyncArrow dir="down" tailLength={40}  size={1.5} class={cn('translate-y-px', Number(remoteToLocalCount) && 'text-primary')} />
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
            loading={syncing === 'lite'}
            disabled={syncing === 'classic'}
            onclick={onSyncClick}
            icon="i-mdi-sync"
            iconProps={{ class: 'size-5' }}>
            {#if syncing === 'lite'}
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
        <SyncArrow dir="up" tailLength={40} size={1.5} class={cn('-translate-y-px', Number(localToRemoteCount) && 'text-primary')}/>
        <span class="text-start" class:font-bold={Number(localToRemoteCount)} class:text-primary={Number(localToRemoteCount)}>{localToRemoteCount ?? '?'}</span>
      </div>
      <div>
        <!-- blank spacer-->
      </div>
  </div>

  <!--  local box-->
  <div class="text-center col-span-full border rounded py-2">
    <Icon icon="i-mdi-cellphone" class="size-10! md:i-mdi-monitor"/>
    <p>{$t`Local`}</p>
  </div>
</div>
