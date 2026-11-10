<script lang="ts">
  import {T, t} from 'svelte-i18n-lingui';
  import {resource} from 'runed';
  import {SyncStatus} from '$lib/dotnet-types/generated-types/LexCore/Sync/SyncStatus';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {onErrorEvent} from '$lib/errors/global-errors';
  import {Icon} from '$lib/components/ui/icon';
  import {FormatRelativeDate} from '$lib/components/ui/format';
  import DevContent from '$lib/layout/DevContent.svelte';
  import flexLogo from '$lib/assets/flex-logo.png';
  import SyncMethodButton from './SyncMethodButton.svelte';
  import NearbyDevicesIcon from './NearbyDevicesIcon.svelte';
  import {shortFolderPath} from './folder-sync-messages';
  import type {SyncMethod} from './sync-target';

  interface Props {
    syncStatus: SyncStatus;
    showClassic: boolean;
    showFolder: boolean;
    /** Bumped by the dialog after each sync; the status reloads when it changes */
    syncCount: number;
    titles: Record<SyncMethod, string>;
    /** The method to focus, e.g. the one the user just came back from */
    focus?: SyncMethod;
    onChoose: (method: SyncMethod) => void;
  }

  const {syncStatus, showClassic, showFolder, syncCount, titles, focus, onChoose}: Props = $props();

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
            <T msg="Auto syncing · Last sync: #">
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
          <span class="relative">
            <img src={flexLogo} alt="" class="size-7" />
            <Icon icon="i-mdi-cloud" class="absolute -bottom-1 -right-1.5 size-4 text-muted-foreground drop-shadow-[0_0_1px_var(--background)]" />
          </span>
        {/snippet}
        {#snippet description()}{$t`Runs on Lexbox, not with FieldWorks on this computer`}{/snippet}
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
    <DevContent>
      <SyncMethodButton title={titles.nearby} focus={focus === 'nearby'} onclick={() => onChoose('nearby')}>
        {#snippet icon()}<NearbyDevicesIcon class="size-7" />{/snippet}
        {#snippet description()}Phones and computers on the same Wi-Fi{/snippet}
      </SyncMethodButton>
    </DevContent>
  </div>
</div>
