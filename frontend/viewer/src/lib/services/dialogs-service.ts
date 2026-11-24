import {useProjectContext} from '$project/project-context.svelte';
import type {IEntry} from '$lib/dotnet-types';
import {useWritingSystemService, type WritingSystemService} from '$project/data';
import type {DeleteDialogOptions} from '$lib/entry-editor/DeleteDialog.svelte';
import type {EntryTemplate, SenseTemplate} from '$lib/entry-editor/NewEntryDialog.svelte';

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

  #invokeManageCustomViews: undefined | (() => void);
  set invokeManageCustomViews(dialog: (() => void) | undefined) {
    this.#invokeManageCustomViews = dialog;
  }

  /** `search.wsId` is the writing system the search text was typed in; it defaults to the project's default vernacular. */
  async createNewEntry(search?: {text: string, wsId?: string}, newEntry?: EntryTemplate, newSense?: SenseTemplate): Promise<IEntry | undefined> {
    if (!this.#invokeNewEntryDialog) throw new Error('No new entry dialog');
    const partialEntry: Partial<IEntry> = {};
    Object.assign(partialEntry, newEntry);
    if (search?.text) {
      const wsId = search.wsId ?? this.writingSystemService.defaultVernacular?.wsId;
      if (wsId === undefined) throw new Error('No default vernacular');
      partialEntry.lexemeForm = {[wsId]: search.text};
    }
    const entry = await this.#invokeNewEntryDialog(partialEntry, newSense);
    return entry;
  }
  async promptDelete(subject: string, subjectDescription?: string, options?: DeleteDialogOptions): Promise<boolean> {
    if (!this.#invokeDeleteDialog) throw new Error('No delete dialog');
    return this.#invokeDeleteDialog(subject, subjectDescription, options);
  }

  openManageCustomViews(): void {
    if (!this.#invokeManageCustomViews) throw new Error('No manage custom views dialog');
    this.#invokeManageCustomViews();
  }
}
