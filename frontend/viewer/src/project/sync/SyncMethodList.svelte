<script lang="ts">
  import {T, t} from 'svelte-i18n-lingui';
  import {resource} from 'runed';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {onErrorEvent} from '$lib/errors/global-errors';
  import {Icon} from '$lib/components/ui/icon';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import flexLogo from '$lib/assets/flex-logo.png';
  import SyncMethodButton from './SyncMethodButton.svelte';
  import NearbyDevicesIcon from './NearbyDevicesIcon.svelte';
  import {shortFolderPath} from './folder-sync-messages';
  import type {SyncMethod} from './sync-target';

  interface Props {
    syncStatus: SyncStatus;
    showClassic: boolean;
    showFolder: boolean;
    showNearby: boolean;
    /** Bumped by the dialog after each sync; the status reloads when it changes */
    syncCount: number;
    titles: Record<SyncMethod, string>;
    /** The method to focus, e.g. the one the user just came back from */
    focus?: SyncMethod;
    onChoose: (method: SyncMethod) => void;
  }

  const {syncStatus, showClassic, showFolder, showNearby, syncCount, titles, focus, onChoose}: Props = $props();

  const service = useSyncStatusService();
  const lastSync = resource(() => syncCount, () => service.getLatestSyncedCommitDate());
  const lastSyncDate = $derived(lastSync.current ? new Date(lastSync.current) : undefined);
  $effect(() => {
    if (lastSync.error) onErrorEvent(lastSync.error);
  });
  const rememberedFolder = resource(() => showFolder, async (show) => show ? service.getRememberedSyncFolder() : undefined);
  const cloudIcon = $derived(syncStatus === SyncStatus.Offline ? 'i-mdi-cloud-off-outline' : 'i-mdi-cloud-outline');
</script>

<div class="flex flex-col gap-4 max-md:mt-8">
  <SyncMethodButton featured title={titles.lite} focus={focus === 'lite'} onclick={() => onChoose('lite')}>
    {#snippet icon()}<Icon icon={cloudIcon} class="size-8" />{/snippet}
    {#snippet description()}
      <span class="flex items-center gap-1.5">
        <span class="size-2 shrink-0 rounded-full"
          class:bg-green-500={syncStatus === SyncStatus.Success}
          class:bg-red-500={syncStatus !== SyncStatus.Success}></span>
        {#if syncStatus === SyncStatus.Success}
          <span>
            <T msg="Auto syncing. Last sync: #">
              {#if !lastSyncDate}
                <span>{$t`Never`}</span>
              {:else}
                <FormatRelativeDate date={lastSyncDate} live />
              {/if}
            </T>
          </span>
        {:else if syncStatus === SyncStatus.Offline}
          {$t`Offline. Your changes are saved on this device.`}
        {:else if syncStatus === SyncStatus.NotLoggedIn}
          {$t`Not logged in`}
        {:else if syncStatus === SyncStatus.NoServer}
          {$t`No server configured`}
        {:else}
          {$t`Error getting sync status`}
        {/if}
      </span>
    {/snippet}
  </SyncMethodButton>
  <div class="flex flex-col divide-y rounded-lg border overflow-hidden">
    {#if showClassic}
      <SyncMethodButton title={titles.classic} focus={focus === 'classic'} onclick={() => onChoose('classic')}>
        {#snippet icon()}
          <svg viewBox="0 0 30 30" class="size-[30px] overflow-visible" aria-hidden="true">
            <defs>
              <path id="classic-cloud" transform="translate(14 13) scale(.8)" d="M6.5 20q-2.28 0-3.89-1.57Q1 16.85 1 14.58q0-1.95 1.17-3.48q1.18-1.53 3.08-1.95q.63-2.3 2.5-3.72Q9.63 4 12 4q2.93 0 4.96 2.04Q19 8.07 19 11q1.73.2 2.86 1.5q1.14 1.28 1.14 3q0 1.88-1.31 3.19T18.5 20Z" />
              <mask id="classic-cloud-notch" maskUnits="userSpaceOnUse" x="0" y="0" width="30" height="30">
                <rect width="30" height="30" fill="#fff" />
                <use href="#classic-cloud" stroke="#000" stroke-width="3.2" stroke-linejoin="round" />
              </mask>
            </defs>
            <image href={flexLogo} width="28" height="28" mask="url(#classic-cloud-notch)" />
            <use href="#classic-cloud" fill="#2f93ee" />
          </svg>
        {/snippet}
        {#snippet description()}{$t`Sync FieldWorks Lite and FieldWorks Classic in Lexbox`}{/snippet}
      </SyncMethodButton>
    {/if}
    {#if showFolder}
      <SyncMethodButton title={titles.folder} focus={focus === 'folder'} onclick={() => onChoose('folder')}>
        {#snippet icon()}<Icon icon="i-mdi-usb-flash-drive-outline" class="size-7" />{/snippet}
        {#snippet description()}
          {#if rememberedFolder.current}
            <span title={rememberedFolder.current}>{$t`Sync with ${shortFolderPath(rememberedFolder.current)}`}</span>
          {:else}
            {$t`Sync with a USB drive or another folder`}
          {/if}
        {/snippet}
      </SyncMethodButton>
    {/if}
    {#if showNearby}
      <SyncMethodButton title={titles.nearby} focus={focus === 'nearby'} onclick={() => onChoose('nearby')}>
        {#snippet icon()}<NearbyDevicesIcon class="size-7" />{/snippet}
        {#snippet description()}Phones and computers on the same Wi-Fi{/snippet}
      </SyncMethodButton>
    {/if}
  </div>
</div>
