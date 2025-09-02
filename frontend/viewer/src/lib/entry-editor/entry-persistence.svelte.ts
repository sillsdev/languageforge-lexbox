import {type Props} from './object-editors/EntryEditor.svelte';
import {useLexboxApi} from '$lib/services/service-provider';
import {useSaveHandler} from '$lib/services/save-event-service.svelte';
import {watch, type Getter} from 'runed';
import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';

export class EntryPersistence {
  lexboxApi = useLexboxApi();
  saveHandler = useSaveHandler();
  initialEntry: IEntry | undefined = undefined;
  constructor(private entryGetter: Getter<IEntry | undefined | null>, private onUpdated: () => void = () => { }) {
    watch(entryGetter, (entry) => {
      if (entry?.id !== this.initialEntry?.id) this.updateInitialEntry(entry);
    });
  }

  get entryEditorProps(): Partial<Props> {
    return {
      onchange: async (changed: { entry: IEntry }) => {
        await this.updateEntry(changed.entry);
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
        //it's ok to use entry getter here because it's probably already been updated before this was called
        this.updateInitialEntry(this.entryGetter());
      }
    };
  }

  public async updateEntry(entry: IEntry): Promise<IEntry> {
    if (this.initialEntry === undefined) throw new Error('Not sure what to compare against');
    if (this.initialEntry.id != entry.id) throw new Error('Entry id mismatch');
    const updatedEntry = await this.saveHandler.handleSave(() => this.lexboxApi.updateEntry(this.initialEntry!, $state.snapshot(entry)));
    this.onUpdated();
    // use the version from the server or else we might get unsaved changes in initialEntry
    this.updateInitialEntry(updatedEntry);
    return updatedEntry;
  }

  public async updateSense(sense: ISense): Promise<ISense> {
    if (this.initialEntry === undefined) throw new Error('Not sure what to compare against');
    const initialSense = this.initialEntry.senses.find(s => s.id === sense.id);
    let updatedSense: ISense;
    const entry: IEntry = this.copy(this.initialEntry);
    if (initialSense) {
      updatedSense = await this.saveHandler.handleSave(() => this.lexboxApi.updateSense(sense.entryId, initialSense, $state.snapshot(sense)));
      entry.senses = entry.senses.map(s => s.id === sense.id ? updatedSense : s);
    } else {
      updatedSense = await this.saveHandler.handleSave(() => this.lexboxApi.createSense(sense.entryId, $state.snapshot(sense)));
      entry.senses.push(updatedSense);
    }
    this.updateInitialEntry(entry);
    return updatedSense;
  }

  public async updateExample(example: IExampleSentence): Promise<IExampleSentence> {
    if (this.initialEntry === undefined) throw new Error('Not sure what to compare against');
    const entry: IEntry = this.copy(this.initialEntry);
    const sense = entry.senses.find(s => s.id === example.senseId);
    if (!sense) throw new Error(`Sense ${example.senseId} not found`);
    const initialExample = sense.exampleSentences.find(e => e.id === example.id);
    let updatedExample: IExampleSentence;
    if (initialExample) {
      updatedExample = await this.saveHandler.handleSave(() => this.lexboxApi.updateExampleSentence(sense.entryId, sense.id, initialExample, $state.snapshot(example)));
      sense.exampleSentences = sense.exampleSentences.map(e => e.id === example.id ? updatedExample : e);
    } else {
      updatedExample = await this.saveHandler.handleSave(() => this.lexboxApi.createExampleSentence(sense.entryId, example.senseId, $state.snapshot(example)));
      sense.exampleSentences.push(updatedExample);
    }
    this.updateInitialEntry(entry);
    return updatedExample;
  }

  private updateInitialEntry(entry: IEntry | undefined | null): void {
    if (!entry) this.initialEntry = undefined;
    else this.initialEntry = this.copy(entry);
  }

  private copy<T>(value: T): T {
    return JSON.parse(JSON.stringify(value)) as T;
  }
}
