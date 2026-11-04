<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {resource} from 'runed';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import {folderSyncMessage} from './folder-sync-messages';
  import type {SyncTarget} from './sync-target';

  interface Props {
    /** Which sync is running, shared with the rest of the dialog so only one runs at a time */
    syncing?: SyncTarget;
    onSynced?: () => void;
  }

  let {syncing = $bindable(), onSynced = () => {}}: Props = $props();

  const service = useSyncStatusService();
  const platformFeatures = usePlatformFeaturesService();
  const rememberedFolder = resource(() => undefined, () => service.getRememberedSyncFolder());
  let running = $state<'picked' | 'remembered'>();

  async function syncWith(path: string) {
    const summary = await service.syncWithFolder(path);
    const {message, type, description} = folderSyncMessage(summary);
    AppNotification.display(message, {type, description, timeout: 'long'});
    onSynced();
    await rememberedFolder.refetch();
  }

  async function run(which: 'picked' | 'remembered', getPath: () => Promise<string | undefined>) {
    syncing = 'folder';
    running = which;
    try {
      const path = await getPath();
      if (path) await syncWith(path);
    } finally {
      syncing = undefined;
      running = undefined;
    }
  }
</script>

<div class="mt-6 pt-4 border-t flex flex-col items-center gap-3">
  <p class="text-foreground/80 text-center flex items-center gap-2">
    <Icon icon="i-mdi-usb-flash-drive-outline" class="size-5" />
    {$t`Sync with a USB drive or another folder`}
  </p>
  <div class="flex flex-wrap justify-center gap-2">
    <Button
      variant="outline"
      icon="i-mdi-folder-sync-outline"
      loading={running === 'picked'}
      disabled={!!syncing && running !== 'picked'}
      onclick={() => void run('picked', () => platformFeatures.service.pickFolder())}>
      {$t`Sync with folder...`}
    </Button>
    {#if rememberedFolder.current}
      {@const path = rememberedFolder.current}
      <Button
        variant="ghost"
        icon="i-mdi-sync"
        title={path}
        loading={running === 'remembered'}
        disabled={!!syncing && running !== 'remembered'}
        onclick={() => void run('remembered', () => Promise.resolve(path))}>
        <span class="max-w-60 truncate">{$t`Sync with ${path} again`}</span>
      </Button>
    {/if}
  </div>
</div>
