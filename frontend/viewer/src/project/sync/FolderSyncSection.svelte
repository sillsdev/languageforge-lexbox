<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {resource} from 'runed';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import {useProjectContext} from '$project/project-context.svelte';
  import type {StorageProp} from '$lib/storage/storage-prop.svelte';
  import {folderSyncMessage, folderDisplayName} from './folder-sync-messages';
  import type {SyncTarget} from './sync-target';

  interface Props {
    /** Which sync is running, shared with the rest of the dialog so only one runs at a time */
    syncing?: SyncTarget;
    onSynced?: () => void;
    /** The device's name for commits with no Lexbox user, owned by the sync view, which also shows and edits it */
    localAuthorName: StorageProp;
    /** Asks for that name; resolves false if the user cancels */
    promptForName: () => Promise<boolean>;
  }

  let {syncing = $bindable(), onSynced = () => {}, localAuthorName, promptForName}: Props = $props();

  const service = useSyncStatusService();
  const platformFeatures = usePlatformFeaturesService();
  const projectContext = useProjectContext();
  const rememberedFolder = resource(() => undefined, () => service.getRememberedSyncFolder());
  let running = $state<'picked' | 'remembered'>();

  // With a Lexbox user the account is the author; without one the device's chosen name signs every change
  const signedInToLexbox = $derived(!!projectContext.projectData?.lastUserId);

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
      if (!path) return;
      if (!signedInToLexbox && !localAuthorName.current && !(await promptForName())) return;
      await syncWith(path);
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
  <p class="text-sm text-muted-foreground text-center">
    {$t`FieldWorks Lite keeps each project in its own folder inside the drive or folder you pick.`}
  </p>
  <div class="flex flex-wrap justify-center items-center gap-2">
    {#if rememberedFolder.current}
      {@const path = rememberedFolder.current}
      <Button
        variant="outline"
        class="border-primary text-primary hover:text-primary"
        icon="i-mdi-sync"
        title={path}
        loading={running === 'remembered'}
        disabled={!!syncing && running !== 'remembered'}
        onclick={() => void run('remembered', () => Promise.resolve(path))}>
        {$t`Sync with ${folderDisplayName(path)} again`}
      </Button>
      <Button
        variant="link"
        loading={running === 'picked'}
        disabled={!!syncing && running !== 'picked'}
        onclick={() => void run('picked', () => platformFeatures.service.pickFolder())}>
        {$t`Sync with another folder...`}
      </Button>
    {:else}
      <Button
        variant="outline"
        icon="i-mdi-folder-sync-outline"
        loading={running === 'picked'}
        disabled={!!syncing && running !== 'picked'}
        onclick={() => void run('picked', () => platformFeatures.service.pickFolder())}>
        {$t`Sync with folder...`}
      </Button>
    {/if}
  </div>
</div>
