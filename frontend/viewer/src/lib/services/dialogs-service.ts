import {useProjectContext} from '$project/project-context.svelte';
import type {IEntry} from '$lib/dotnet-types';
import {useWritingSystemService, type WritingSystemService} from '$project/data';
import type {DeleteDialogOptions} from '$lib/entry-editor/DeleteDialog.svelte';
import type {SenseTemplate} from '$lib/entry-editor/NewEntryDialog.svelte';

const symbol = Symbol.for('fw-lite-dialogs');

export function useDialogsService() {
  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();
  return projectContext.getOrAdd(symbol, () => new DialogsService(writingSystemService));
}

export class DialogsService {
  constructor(private writingSystemService: WritingSystemService) {
  }

  #invokeDeleteDialog: undefined | ((subject: string, subjectDescription?: string, options?: DeleteDialogOptions) => Promise<boolean>);
  set invokeDeleteDialog(dialog: ((subject: string, subjectDescription?: string, options?: DeleteDialogOptions) => Promise<boolean>) | undefined) {
    this.#invokeDeleteDialog = dialog;
  }
  #invokeNewEntryDialog: undefined | ((newEntry: Partial<IEntry>, newSense?: SenseTemplate) => Promise<IEntry | undefined>);
  set invokeNewEntryDialog(dialog: ((newEntry: Partial<IEntry>, newSense?: SenseTemplate) => Promise<IEntry | undefined>)) {
    this.#invokeNewEntryDialog = dialog;
  }

  async createNewEntry(headword?: string, newSense?: SenseTemplate): Promise<IEntry | undefined> {
    if (!this.#invokeNewEntryDialog) throw new Error('No new entry dialog');
    const partialEntry: Partial<IEntry> = {};
    if (headword) {
      const defaultWs = this.writingSystemService.defaultVernacular?.wsId;
      if (defaultWs === undefined) throw new Error('No default vernacular');
      partialEntry.lexemeForm = {[defaultWs]: headword};
    }
    const entry = await this.#invokeNewEntryDialog(partialEntry, newSense);
    return entry;
  }
  async promptDelete(subject: string, subjectDescription?: string, options?: DeleteDialogOptions): Promise<boolean> {
    if (!this.#invokeDeleteDialog) throw new Error('No delete dialog');
    return this.#invokeDeleteDialog(subject, subjectDescription, options);
  }
}
