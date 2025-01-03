import type DeleteDialog from '$lib/entry-editor/DeleteDialog.svelte';
import {getContext, setContext} from 'svelte';

export function initDialogService(rootDialog: () => DeleteDialog | undefined): DialogService {
  const dialogService = new DialogService(rootDialog);
  setContext('DialogService', dialogService);
  return dialogService;
}

export function useDialogService(): DialogService {
  const rootDialog = getContext('DialogService');
  if (!rootDialog) {
    throw new Error('DialogService not found');
  }
  return rootDialog as DialogService;
}

export class DialogService {

  constructor(private deleteDialog: () => DeleteDialog | undefined) {
  }

  promptDelete(subject: string): Promise<boolean> {
    const deleteDialog = this.deleteDialog();
    if (!deleteDialog) throw new Error('no deleted dialog found');
    return deleteDialog.prompt(subject);
  }
}
