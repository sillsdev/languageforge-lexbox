import {FolderSyncStatus} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/FolderSyncStatus';
import type {IFolderSyncSummary} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/IFolderSyncSummary';
import {gt} from 'svelte-i18n-lingui';

export interface FolderSyncMessage {
  message: string;
  type: 'success' | 'warning';
  description?: string;
}

/** Keeps the drive and the last folder of a long path, e.g. `C:\…\sena-3`. */
export function shortFolderPath(path: string, maxLength = 40): string {
  if (path.length <= maxLength) return path;
  const separator = path.includes('\\') ? '\\' : '/';
  const parts = path.split(separator);
  return [parts[0], '…', parts.at(-1)].join(separator);
}

/**
 * Describes a folder sync for a toast. Every refusal is expected (wrong folder, newer app), so none is an error.
 * @param projectCode the open project's, which names the folder a sync uses inside the picked one
 */
export function folderSyncMessage(summary: IFolderSyncSummary, projectCode?: string): FolderSyncMessage {
  switch (summary.status) {
    case FolderSyncStatus.Synced:
      return {
        message: gt`Synced with the folder. You can remove the drive.`,
        type: 'success',
        description: summary.mediaFailed
          ? gt`Some pictures or audio files couldn't be copied. They will be copied the next time you sync.`
          : undefined,
      };
    case FolderSyncStatus.NotASyncFolder:
      return {
        message: projectCode
          ? gt`This folder already has a folder called ${projectCode}, but it doesn't hold a FieldWorks Lite project. Choose another folder.`
          : gt`This folder already has a folder named after this project, but it doesn't hold a FieldWorks Lite project. Choose another folder.`,
        type: 'warning',
      };
    case FolderSyncStatus.NewerAppNeeded:
      return {message: gt`A newer version of FieldWorks Lite wrote this folder. Update FieldWorks Lite to sync with it.`, type: 'warning'};
    case FolderSyncStatus.DifferentProject:
      return {
        message: gt`This folder holds a different project, ${summary.folderProjectName ?? ''}, not the one that is open. To open that project, go to the home screen and choose Get project from folder.`,
        type: 'warning',
      };
    case FolderSyncStatus.DifferentHistory:
      return {
        message: gt`The copy of ${summary.folderProjectName ?? ''} in this folder didn't come from the same original as the copy on this device, so the two can't be combined. Choose another folder.`,
        type: 'warning',
      };
    case FolderSyncStatus.NoWritingSystemInCommon: {
      const folderWs = summary.folderWritingSystemIds?.join(', ') ?? '';
      const localWs = summary.localWritingSystemIds?.join(', ') ?? '';
      return {
        message: gt`The project in this folder uses different writing systems (${folderWs}) from the copy on this device (${localWs}), so the two can't be synced. Choose another folder, or compare the writing systems in each copy.`,
        type: 'warning',
      };
    }
    case FolderSyncStatus.Failed:
      return {message: gt`Couldn't finish syncing with the folder. Nothing on this device was lost. Check that the drive is still plugged in, then try again.`, type: 'warning'};
  }
}
