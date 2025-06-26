import {type Props} from './object-editors/EntryEditor.svelte';
import {useLexboxApi} from '$lib/services/service-provider';
import {useSaveHandler} from '$lib/services/save-event-service.svelte';
import {watch, type Getter} from 'runed';
import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';

export class EntryPersistence {
  lexboxApi = useLexboxApi();
  saveHandler = useSaveHandler();
  initialEntry: IEntry | undefined = undefined;
  constructor(private entryGetter: Getter<IEntry | undefined>, private onUpdated: () => void = () => { }) {
    watch(entryGetter, (entry) => {
      if (entry?.id !== this.initialEntry?.id) this.updateInitialEntry();
    });
  }

  get entryEditorProps(): Partial<Props> {
    return {
      onchange: async (changed: { entry: IEntry }) => {
        const updatedEntry = await this.updateEntry(changed.entry);
        this.onUpdated();
        // use the version from the server or else we might get unsaved changes in initialEntry
        this.updateInitialEntry(updatedEntry);
      },
      ondelete: async (e: { entry: IEntry, example?: IExampleSentence, sense?: ISense }) => {
        if (e.example !== undefined && e.sense !== undefined) {
          await this.saveHandler.handleSave(() => this.lexboxApi.deleteExampleSentence(e.entry.id, e.sense!.id, e.example!.id));
        } else if (e.sense !== undefined) {
          await this.saveHandler.handleSave(() => this.lexboxApi.deleteSense(e.entry.id, e.sense!.id));
        } else {
          await this.saveHandler.handleSave(() => this.lexboxApi.deleteEntry(e.entry.id));
          this.onUpdated();
          return;
        }
        this.updateInitialEntry();
      }
    };
  }

  private async updateEntry(updatedEntry: IEntry): Promise<IEntry> {
    if (this.initialEntry === undefined) throw new Error('Not sure what to compare against');
    if (this.initialEntry.id != updatedEntry.id) throw new Error('Entry id mismatch');
    return await this.saveHandler.handleSave(() => this.lexboxApi.updateEntry(this.initialEntry!, $state.snapshot(updatedEntry)));
  }

  private updateInitialEntry(entry = this.entryGetter()): void {
    if (!entry) this.initialEntry = undefined;
    else this.initialEntry = JSON.parse(JSON.stringify(entry)) as IEntry;
  }
}
