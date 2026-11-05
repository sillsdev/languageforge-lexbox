<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {navigate} from 'svelte-routing';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useProjectsService} from '$lib/services/service-provider';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import {OpenFromFolderOutcome} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/OpenFromFolderOutcome';
  import type {IOpenFromFolderSummary} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/IOpenFromFolderSummary';
  import {folderSyncMessage} from '../project/sync/folder-sync-messages';
  import FolderPicker from '../project/sync/FolderPicker.svelte';

  const {refreshProjects}: {refreshProjects: () => Promise<void>} = $props();

  const projectsService = useProjectsService();
  const platformFeatures = usePlatformFeaturesService();
  let loading = $state(false);
  let folderPicker = $state<FolderPicker>();

  async function openFromFolder() {
    const path = await folderPicker!.pick();
    if (!path) return;
    loading = true;
    try {
      const result = await projectsService.openFromFolder(path);
      await refreshProjects();
      report(result);
      if (result.projectCode && (result.outcome === OpenFromFolderOutcome.Downloaded || result.outcome === OpenFromFolderOutcome.Synced)) {
        navigate(`/project/${result.projectCode}`);
      }
    } finally {
      loading = false;
    }
  }

  function report(result: IOpenFromFolderSummary) {
    switch (result.outcome) {
      case OpenFromFolderOutcome.Downloaded:
        AppNotification.display($t`Opened ${result.projectName ?? result.projectCode ?? ''} from the folder. You can remove the drive.`, {type: 'success', timeout: 'long'});
        return;
      case OpenFromFolderOutcome.Synced:
      case OpenFromFolderOutcome.Refused:
        if (result.sync) {
          const {message, type, description} = folderSyncMessage(result.sync);
          AppNotification.display(message, {type, description, timeout: 'long'});
        }
        return;
      case OpenFromFolderOutcome.EmptyFolder:
        AppNotification.display($t`This folder is empty. To put a project in it, open that project on a device that has it and choose Sync with a folder.`, {type: 'warning', timeout: 'long'});
        return;
      case OpenFromFolderOutcome.NotASyncFolder:
        AppNotification.display($t`This folder doesn't hold a FieldWorks Lite project. Choose the project's own folder, not the drive or folder it's in.`, {type: 'warning', timeout: 'long'});
        return;
      case OpenFromFolderOutcome.ProjectCodeInUse:
        AppNotification.display($t`${result.projectName ?? ''} can't be added: this device already has a different project with the same code, ${result.projectCode ?? ''}.`, {type: 'warning', timeout: 'long'});
        return;
      case OpenFromFolderOutcome.NewerAppNeeded:
        AppNotification.display($t`A newer version of FieldWorks Lite wrote this folder. Update FieldWorks Lite to open it.`, {type: 'warning', timeout: 'long'});
        return;
    }
  }
</script>

{#if platformFeatures.features.supportsFolderSync}
  <ListItem icon="i-mdi-folder-open-outline" class="mb-2" onclick={() => void openFromFolder()} {loading}>
    <span>{$t`Get project from folder...`}</span>
    <span class="text-sm text-muted-foreground">{$t`From a USB drive or shared folder`}</span>
  </ListItem>
{/if}
<FolderPicker bind:this={folderPicker} />
