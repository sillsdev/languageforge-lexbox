<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {resource} from 'runed';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {folderSyncMessage, shortFolderPath} from './folder-sync-messages';
  import FolderPicker from './FolderPicker.svelte';
  import type {SyncTarget} from './sync-target';

  interface Props {
    /** Which sync is running, shared with the rest of the dialog so only one runs at a time */
    syncing?: SyncTarget;
    onSynced?: () => void;
  }

  let {syncing = $bindable(), onSynced = () => {}}: Props = $props();

  const service = useSyncStatusService();
  const rememberedFolder = resource(() => undefined, () => service.getRememberedSyncFolder());
  let running = $state<'picked' | 'remembered'>();
  let folderPicker = $state<FolderPicker>();

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
  <div class="flex flex-col items-center gap-1">
    {#if rememberedFolder.current}
      {@const path = rememberedFolder.current}
      <Button
        variant="outline"
        class="border-primary text-primary hover:text-primary"
        icon="i-mdi-sync"
        iconProps={{ class: 'size-5' }}
        title={path}
        loading={running === 'remembered'}
        disabled={!!syncing && running !== 'remembered'}
        onclick={() => void run('remembered', () => Promise.resolve(path))}>
        {#if running === 'remembered'}
          {$t`Syncing...`}
        {:else}
          {$t`Sync with ${shortFolderPath(path)}`}
        {/if}
      </Button>
      <Button
        variant="link"
        loading={running === 'picked'}
        disabled={!!syncing && running !== 'picked'}
        onclick={() => void run('picked', () => folderPicker!.pick(rememberedFolder.current))}>
        {$t`Sync with other folder...`}
      </Button>
    {:else}
      <Button
        variant="outline"
        icon="i-mdi-folder-sync-outline"
        loading={running === 'picked'}
        disabled={!!syncing && running !== 'picked'}
        onclick={() => void run('picked', () => folderPicker!.pick(rememberedFolder.current))}>
        {$t`Sync with folder...`}
      </Button>
    {/if}
  </div>
</div>
<FolderPicker bind:this={folderPicker} />
