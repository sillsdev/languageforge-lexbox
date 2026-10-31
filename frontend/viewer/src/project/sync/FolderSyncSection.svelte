<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {resource} from 'runed';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useSyncStatusService} from '$lib/services/sync-status-service';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import {useProjectContext} from '$project/project-context.svelte';
  import LocalAuthorNameDialog from '$lib/author/LocalAuthorNameDialog.svelte';
  import {useLocalAuthorName} from '$lib/author/local-author-name.svelte';
  import {folderSyncMessage, folderDisplayName} from './folder-sync-messages';
  import type {SyncTarget} from './sync-target';

  interface Props {
    /** Which sync is running, shared with the rest of the dialog so only one runs at a time */
    syncing?: SyncTarget;
    onSynced?: () => void;
  }

  let {syncing = $bindable(), onSynced = () => {}}: Props = $props();

  const service = useSyncStatusService();
  const platformFeatures = usePlatformFeaturesService();
  const projectContext = useProjectContext();
  const localAuthorName = useLocalAuthorName();
  const rememberedFolder = resource(() => undefined, () => service.getRememberedSyncFolder());
  let running = $state<'picked' | 'remembered'>();
  let nameDialog = $state<LocalAuthorNameDialog>();

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
      if (!signedInToLexbox && !localAuthorName.current && !(await nameDialog!.prompt())) return;
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
  {#if !signedInToLexbox && !localAuthorName.loading}
    <p class="text-sm text-muted-foreground flex items-center gap-1">
      {#if localAuthorName.current}
        {$t`Your changes are signed as ${localAuthorName.current}`}
        <Button variant="ghost" size="xs" onclick={() => void nameDialog!.prompt()}>{$t`Change`}</Button>
      {:else}
        {$t`Your changes aren't signed with a name yet`}
        <Button variant="ghost" size="xs" onclick={() => void nameDialog!.prompt()}>{$t`Add your name`}</Button>
      {/if}
    </p>
  {/if}
</div>
<LocalAuthorNameDialog {localAuthorName} bind:this={nameDialog} />
