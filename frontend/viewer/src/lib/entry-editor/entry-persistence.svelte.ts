import { type Props } from './object-editors/EntryEditor.svelte';
import { useLexboxApi } from '$lib/services/service-provider';
import { useSaveHandler } from '$lib/services/save-event-service.svelte';
import type { Getter } from 'runed';
import type { IEntry, IExampleSentence, ISense } from '$lib/dotnet-types';

export class EntryPersistence {
  lexboxApi = useLexboxApi();
  saveHandler = useSaveHandler();
  initialEntry: IEntry | undefined = undefined;
  constructor(private entryGetter: Getter<IEntry | undefined>, private onRefresh: () => void = () => { }) {
    $effect(() => {
      if (this.initialEntry) return;
      if (entryGetter()) this.updateInitialEntry();
    });
  }

  private get entry(): IEntry {
    const entry = this.entryGetter();
    if (!entry) throw new Error('Entry not found');
    return entry;
  }

  get entryEditorProps(): Partial<Props> {
    return {
      onchange: async (changed: { entry: IEntry }) => {
        await this.updateEntry(changed.entry);
        this.onRefresh();
        this.updateInitialEntry();
      },
      ondelete: async (e: { entry: IEntry, example?: IExampleSentence, sense?: ISense }) => {
        if (e.example !== undefined && e.sense !== undefined) {
          await this.saveHandler.handleSave(() => this.lexboxApi.deleteExampleSentence(e.entry.id, e.sense!.id, e.example!.id));
        } else if (e.sense !== undefined) {
          await this.saveHandler.handleSave(() => this.lexboxApi.deleteSense(e.entry.id, e.sense!.id));
        } else {
          await this.saveHandler.handleSave(() => this.lexboxApi.deleteEntry(e.entry.id));
          this.onRefresh();
          return;
        }
        this.updateInitialEntry();
      }
    };
  }

  async updateEntry(updatedEntry: IEntry) {
    if (this.initialEntry === undefined) throw new Error('Not sure what to compare against');
    if (this.initialEntry.id != updatedEntry.id) throw new Error('Entry id mismatch');
    await this.saveHandler.handleSave(() => this.lexboxApi.updateEntry(this.initialEntry!, updatedEntry));
  }

  updateInitialEntry() {
    this.initialEntry = JSON.parse(JSON.stringify(this.entry)) as IEntry;
  }
}
