<script lang="ts">
  import ListItem from '$lib/components/ListItem.svelte';
  import {t} from 'svelte-i18n-lingui';
  import {navigate} from 'svelte-routing';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useProjectsService} from '$lib/services/service-provider';
  import {usePlatformFeaturesService} from '$lib/services/platform-features-service';
  import {OpenFromFolderOutcome} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/OpenFromFolderOutcome';
  import type {IOpenFromFolderResult} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/IOpenFromFolderResult';
  import {folderSyncMessage} from '../project/sync/folder-sync-messages';
  import FolderPicker from '../project/sync/FolderPicker.svelte';
  import {hasFlag} from '$lib/feature-flags/feature-flags.svelte';
  import NameDialog from '$lib/author/NameDialog.svelte';
  import {useLocalAuthorName} from '$lib/author/local-author-name.svelte';

  const {refreshProjects}: {refreshProjects: () => Promise<void>} = $props();

  const projectsService = useProjectsService();
  const platformFeatures = usePlatformFeaturesService();
  let loading = $state(false);
  let folderPicker = $state<FolderPicker>();
  let nameDialog = $state<NameDialog>();
  const authorName = useLocalAuthorName();

  async function openFromFolder() {
    if (!authorName.current && !await nameDialog!.ask()) return;
    const path = await folderPicker!.pick();
    if (!path) return;
    loading = true;
    try {
      const result = await projectsService.openFromFolder(path);
      await refreshProjects();
      report(result, path);
      if (result.projectCode && (result.outcome === OpenFromFolderOutcome.Downloaded || result.outcome === OpenFromFolderOutcome.AlreadyOnDevice)) {
        navigate(`/project/${result.projectCode}`);
      }
    } finally {
      loading = false;
    }
  }

  async function syncNow(projectCode: string, path: string) {
    const syncing = AppNotification.loading($t`Syncing with the folder...`);
    try {
      const summary = await projectsService.syncProjectWithFolder(projectCode, path);
      const {message, type, description} = folderSyncMessage(summary, projectCode);
      AppNotification.display(message, {type, description, timeout: 'long'});
    } finally {
      syncing.dismiss();
    }
  }

  function report(result: IOpenFromFolderResult, path: string) {
    switch (result.outcome) {
      case OpenFromFolderOutcome.Downloaded:
        AppNotification.display($t`Opened ${result.projectName ?? result.projectCode ?? ''} from the folder. You can remove the drive.`, {type: 'success', timeout: 'long'});
        return;
      case OpenFromFolderOutcome.AlreadyOnDevice: {
        const projectCode = result.projectCode ?? '';
        AppNotification.displayAction(
          $t`${result.projectName ?? projectCode} is already on this device, so it was opened without syncing.`,
          {label: $t`Sync now`, callback: () => { void syncNow(projectCode, path); return {dismiss: true}; }},
          'info');
        return;
      }
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

{#if platformFeatures.features.supportsFolderSync && hasFlag('folder-sync')}
  <ListItem icon="i-mdi-folder-open-outline" class="mb-2" onclick={() => void openFromFolder()} {loading}>
    <span>{$t`Get project from folder...`}</span>
    <span class="text-sm text-muted-foreground">{$t`From a USB drive or shared folder`}</span>
  </ListItem>
{/if}
<NameDialog bind:this={nameDialog} />
<FolderPicker bind:this={folderPicker} />
