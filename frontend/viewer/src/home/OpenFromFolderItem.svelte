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

  const {refreshProjects}: {refreshProjects: () => Promise<void>} = $props();

  const projectsService = useProjectsService();
  const platformFeatures = usePlatformFeaturesService();
  let loading = $state(false);

  async function openFromFolder() {
    const path = await platformFeatures.service.pickFolder();
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
        AppNotification.display($t`Opened ${result.projectCode ?? ''} from the folder. You can remove the drive.`, {type: 'success', timeout: 'long'});
        return;
      case OpenFromFolderOutcome.Synced:
      case OpenFromFolderOutcome.Refused:
        if (result.sync) {
          const {message, type, description} = folderSyncMessage(result.sync);
          AppNotification.display(message, {type, description, timeout: 'long'});
        }
        return;
      case OpenFromFolderOutcome.EmptyFolder:
        AppNotification.display($t`This folder is empty. Sync a project with it first, from the project's sync dialog.`, {type: 'warning', timeout: 'long'});
        return;
      case OpenFromFolderOutcome.NotASyncFolder:
        AppNotification.display($t`This folder doesn't hold a FieldWorks Lite project.`, {type: 'warning', timeout: 'long'});
        return;
      case OpenFromFolderOutcome.ProjectCodeInUse:
        AppNotification.display($t`A different project with the code ${result.projectCode ?? ''} is already on this device.`, {type: 'warning', timeout: 'long'});
        return;
      case OpenFromFolderOutcome.NewerAppNeeded:
        AppNotification.display($t`A newer version of FieldWorks Lite wrote this folder. Update FieldWorks Lite to open it.`, {type: 'warning', timeout: 'long'});
        return;
    }
  }
</script>

{#if platformFeatures.features.supportsFolderSync}
  <ListItem icon="i-mdi-folder-open-outline" class="mb-2" onclick={() => void openFromFolder()} {loading}>
    <span>{$t`Open from folder...`}</span>
    <span class="text-sm text-muted-foreground">{$t`Get a project from a USB drive or another folder`}</span>
  </ListItem>
{/if}
