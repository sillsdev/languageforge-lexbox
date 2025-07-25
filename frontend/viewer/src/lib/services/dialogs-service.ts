import {useProjectContext} from '$lib/project-context.svelte';
import type {IEntry} from '$lib/dotnet-types';
import {useWritingSystemService, type WritingSystemService} from '$lib/writing-system-service.svelte';

const symbol = Symbol.for('fw-lite-dialogs');

export function useDialogsService() {
  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();
  return projectContext.getOrAdd(symbol, () => new DialogsService(writingSystemService));
}

export class DialogsService {
  constructor(private writingSystemService: WritingSystemService) {
  }

  #invokeDeleteDialog: undefined | ((subject: string, subjectDescription?: string) => Promise<boolean>);
  set invokeDeleteDialog(dialog: ((subject: string, subjectDescription?: string) => Promise<boolean>)) {
    this.#invokeDeleteDialog = dialog;
  }
  #invokeNewEntryDialog: undefined | ((newEntry: Partial<IEntry>) => Promise<IEntry | undefined>);
  set invokeNewEntryDialog(dialog: ((newEntry: Partial<IEntry>) => Promise<IEntry | undefined>)) {
    this.#invokeNewEntryDialog = dialog;
  }
  #invokeAudioDialog: undefined | (() => Promise<string | undefined>);
  set invokeAudioDialog(dialog: (() => Promise<string | undefined>)) {
    this.#invokeAudioDialog = dialog;
  }

  async createNewEntry(headword?: string): Promise<IEntry | undefined> {
    if (!this.#invokeNewEntryDialog) throw new Error('No new entry dialog');
    const partialEntry: Partial<IEntry> = {};
    if (headword) {
      const defaultWs = this.writingSystemService.defaultVernacular?.wsId;
      if (defaultWs === undefined) throw new Error('No default vernacular');
      partialEntry.lexemeForm = {[defaultWs]: headword};
    }
    const entry = await this.#invokeNewEntryDialog(partialEntry);
    return entry;
  }
  async promptDelete(subject: string, subjectDescription?: string): Promise<boolean> {
    if (!this.#invokeDeleteDialog) throw new Error('No delete dialog');
    return this.#invokeDeleteDialog(subject, subjectDescription);
  }
  async getAudio(): Promise<string | undefined> {
    if (!this.#invokeAudioDialog) throw new Error('No audio dialog');
    return this.#invokeAudioDialog();
  }
}
