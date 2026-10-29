import {FolderSyncStatus} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/FolderSyncStatus';
import type {IFolderSyncSummary} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/IFolderSyncSummary';
import {gt} from 'svelte-i18n-lingui';

export interface FolderSyncMessage {
  message: string;
  type: 'success' | 'warning';
  description?: string;
}

/** Describes a folder sync for a toast. Every refusal is expected (wrong folder, newer app), so none is an error. */
export function folderSyncMessage(summary: IFolderSyncSummary): FolderSyncMessage {
  switch (summary.status) {
    case FolderSyncStatus.Synced:
      return {
        message: gt`Synced with the folder. You can remove the drive.`,
        type: 'success',
        description: summary.mediaFailed
          ? gt`Some media files couldn't be copied. They will be copied the next time you sync.`
          : undefined,
      };
    case FolderSyncStatus.NotASyncFolder:
      return {message: gt`This folder has other files in it. Choose an empty folder or one used for syncing before.`, type: 'warning'};
    case FolderSyncStatus.NewerAppNeeded:
      return {message: gt`A newer version of FieldWorks Lite wrote this folder. Update FieldWorks Lite to sync with it.`, type: 'warning'};
    case FolderSyncStatus.DifferentProject:
      return {
        message: gt`This folder holds a different project: ${summary.folderProjectName ?? ''}. Use Open from folder on the home screen to open it.`,
        type: 'warning',
      };
    case FolderSyncStatus.DifferentHistory:
      return {message: gt`This folder holds a separate copy of this project that can't be merged with yours. Choose another folder.`, type: 'warning'};
    case FolderSyncStatus.NoWritingSystemInCommon:
      return {message: gt`This folder's copy of the project shares no writing system with yours, so it can't be synced.`, type: 'warning'};
    case FolderSyncStatus.Failed:
      return {message: gt`Couldn't sync with the folder. Your local changes are safe.`, type: 'warning'};
  }
}
