/* eslint-disable @typescript-eslint/naming-convention */
import {
  type IEntry, type IExampleSentence, type ISense, type JsonPatch,
  type LexboxApi,
  type QueryOptions,
  type WritingSystems
} from 'viewer/lexbox-api';


export class LfClassicLexboxApi implements LexboxApi {
  constructor(private projectCode: string) {
  }

  async GetWritingSystems(): Promise<WritingSystems> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/writingSystems`);
    return (await result.json()) as WritingSystems;
  }

  GetEntriesForExemplar(exemplar: string, options: QueryOptions | undefined): Promise<IEntry[]> {
    throw new Error('Method not implemented.');
  }

  async GetEntries(options: QueryOptions | undefined): Promise<IEntry[]> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/entries?order=desc&count=100`);
    return (await result.json()) as IEntry[];
  }

  SearchEntries(query: string, options: QueryOptions | undefined): Promise<IEntry[]> {
    throw new Error('Method not implemented.');
  }

  GetEntry(guid: string): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  CreateEntry(entry: IEntry): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  UpdateEntry(guid: string, update: JsonPatch): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  CreateSense(entryGuid: string, sense: ISense): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  UpdateSense(entryGuid: string, senseGuid: string, update: JsonPatch): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  CreateExampleSentence(entryGuid: string, senseGuid: string, exampleSentence: IExampleSentence): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  UpdateExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string, update: JsonPatch): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  GetExemplars(): Promise<string[]> {
    throw new Error('Method not implemented.');
  }

  DeleteEntry(guid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  DeleteSense(entryGuid: string, senseGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  DeleteExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

}
