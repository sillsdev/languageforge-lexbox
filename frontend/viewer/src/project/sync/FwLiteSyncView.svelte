<script lang="ts">
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import {Icon} from '$lib/components/ui/icon';
  import LoginButton from '$lib/auth/LoginButton.svelte';
  import {Button} from '$lib/components/ui/button';
  import type {ILexboxServer} from '$lib/dotnet-types';
  import {fade} from 'svelte/transition';
  import {t, T} from 'svelte-i18n-lingui';
  import SyncArrow from './SyncArrow.svelte';
  import {cn} from '$lib/utils';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {onErrorEvent} from '$lib/errors/global-errors';
  import {resource} from 'runed';
  import type {SyncTarget} from './sync-target';
  import FolderSyncSection from './FolderSyncSection.svelte';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';

  import * as Popover from '$lib/components/ui/popover';

  interface Props {
    syncStatus: SyncStatus;
    server?: ILexboxServer;
    serverId?: string;
    projectCode?: string;
    /** Which sync is running, shared with the other tab so only one runs at a time */
    syncing?: SyncTarget;
    /** Bumped by the dialog after each sync; the status reloads when it changes */
    syncCount?: number;
    /** The user can't sync with FieldWorks Classic, so tell them how its changes still reach them */
    explainFieldWorksSync?: boolean;
    onLoginStatusChange?: (status: 'logged-in' | 'logged-out') => void;
  }

  let {
    syncStatus,
    server,
    serverId,
    projectCode,
    syncing = $bindable(),
    syncCount = 0,
    explainFieldWorksSync = false,
    onLoginStatusChange = () => {},
  }: Props = $props();

  const service = useSyncStatusService();
  const platformFeatures = usePlatformFeaturesService();
  const status = resource(() => syncCount, async (_, _prev, {signal}) => {
    const [pendingCommits, latestSyncedCommitDate] = await Promise.all([
      service.getLocalStatus(),
      service.getLatestSyncedCommitDate(),
    ]);
    // Our calls can't be cancelled, so drop the result if a newer load has started
    signal.throwIfAborted();
    return {pendingCommits, latestSyncedCommitDate};
  });
  // Local status shouldn't fail, so report it like any unexpected error
  $effect(() => {
    if (status.error) onErrorEvent(status.error);
  });

  let remoteToLocalCount = $derived(status.current?.pendingCommits?.remote);
  let localToRemoteCount = $derived(status.current?.pendingCommits?.local);
  let lastLocalSyncDate = $derived(status.current?.latestSyncedCommitDate ? new Date(status.current.latestSyncedCommitDate) : undefined);
  const serverName = $derived(server?.displayName ?? serverId ?? 'unknown');
  const serverProjectUrl = $derived(`${server?.authority}/project/${encodeURIComponent(projectCode ?? '')}`);
  const heading = $derived($t`Sync your changes with other FieldWorks Lite users`);
  const cloudIcon = $derived(syncStatus === SyncStatus.Offline ? 'i-mdi-cloud-off-outline' : 'i-mdi-cloud-outline');

  async function sync() {
    const result = await service.triggerCrdtSync();
    if (!result.isSynced) {
      // Couldn't sync (offline, server unreachable, or not logged in): the backend has already published the
      // real sync status, which re-renders this view, so give gentle feedback instead of optimistically
      // zeroing the counts.
      AppNotification.display($t`Couldn't sync right now — your local changes are safe.`, {type: 'warning', timeout: 'short'});
      await status.refetch();
      return;
    }
    // Optimistically update status, then query it
    if (status.current) status.mutate({...status.current, pendingCommits: {local: 0, remote: 0}});
    await status.refetch();
  }

  function onSyncClick() {
    syncing = 'lite';
    void sync().finally(() => {
      syncing = undefined;
    });
  }
</script>
<p class="mb-6 text-lg text-center">
  {#if explainFieldWorksSync}
    <Popover.Root>
      <Popover.InfoTrigger>
        {heading}
      </Popover.InfoTrigger>
      <Popover.Content class="max-w-md text-sm">
        {$t`Some people may work on this project in FieldWorks Classic. Changes only move between FieldWorks Lite and FieldWorks Classic when someone with permission syncs them.`}
      </Popover.Content>
    </Popover.Root>
  {:else}
    {heading}
  {/if}
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
            disabled={!!syncing && syncing !== 'lite'}
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
{#if platformFeatures.features.supportsFolderSync}
  <FolderSyncSection bind:syncing onSynced={() => void status.refetch()} />
{/if}
