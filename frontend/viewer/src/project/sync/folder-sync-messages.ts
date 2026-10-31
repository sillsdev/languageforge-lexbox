import {FolderSyncStatus} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/FolderSyncStatus';
import type {IFolderSyncSummary} from '$lib/dotnet-types/generated-types/FwLiteShared/LocalSync/IFolderSyncSummary';
import {gt} from 'svelte-i18n-lingui';

export interface FolderSyncMessage {
  message: string;
  type: 'success' | 'warning';
  description?: string;
}

/** The folder's own name, as Explorer shows it; the full path for a bare drive root. */
export function folderDisplayName(path: string): string {
  return path.split(/[\\/]/).filter(Boolean).at(-1) ?? path;
}

/** Describes a folder sync for a toast. Every refusal is expected (wrong folder, newer app), so none is an error. */
export function folderSyncMessage(summary: IFolderSyncSummary): FolderSyncMessage {
  switch (summary.status) {
    case FolderSyncStatus.Synced:
      return {
        message: gt`Synced with ${summary.folderPath}. You can remove the drive.`,
        type: 'success',
        description: summary.mediaFailed
          ? gt`Some media files couldn't be copied. They will be copied the next time you sync.`
          : undefined,
      };
    case FolderSyncStatus.DamagedManifest:
      return {message: gt`FieldWorks Lite can't read the project files in this folder. Choose a different folder.`, type: 'warning'};
    case FolderSyncStatus.NewerAppNeeded:
      return {message: gt`A newer version of FieldWorks Lite wrote this folder. Update FieldWorks Lite to sync with it.`, type: 'warning'};
    case FolderSyncStatus.DifferentProject: {
      const name = summary.folderProjectName ?? '';
      return {
        message: gt`This folder holds a different project: ${name}. To sync this project, pick the drive or folder that contains it instead. To open ${name}, use Open from folder on the home screen.`,
        type: 'warning',
      };
    }
    case FolderSyncStatus.DifferentHistory:
      return {message: gt`This folder holds a separate copy of this project that can't be merged with yours. Choose another folder.`, type: 'warning'};
    case FolderSyncStatus.NoWritingSystemInCommon:
      return {message: gt`This folder's copy of the project shares no writing system with yours, so it can't be synced.`, type: 'warning'};
    case FolderSyncStatus.Failed:
      return {message: gt`Couldn't sync with the folder. Your local changes are safe.`, type: 'warning'};
  }
}
