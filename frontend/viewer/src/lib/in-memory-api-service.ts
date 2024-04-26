import type {
  IEntry,
  IExampleSentence,
  ISense,
  JsonPatch,
  LexboxApi,
  QueryOptions,
  WritingSystems
} from './services/lexbox-api';
import {entries, writingSystems} from './entry-data';

export class InMemoryApiService implements LexboxApi {
  GetEntries(options: QueryOptions | undefined): Promise<IEntry[]> {
    return Promise.resolve(this.ApplyQueryOptions(entries, options));
  }

  GetWritingSystems(): Promise<WritingSystems> {
    return Promise.resolve(writingSystems);
  }

  SearchEntries(query: string, options: QueryOptions | undefined): Promise<IEntry[]> {
    return Promise.resolve(this.ApplyQueryOptions(entries.filter(entry =>
      [
        ...Object.values(entry.lexemeForm ?? {}),
        ...Object.values(entry.citationForm ?? {}),
        ...Object.values(entry.literalMeaning ?? {}),
      ].some(value => value?.toLowerCase().includes(query.toLowerCase()))), options));
  }

  private ApplyQueryOptions(entries: IEntry[], options: QueryOptions | undefined): IEntry[] {
    if (!options) return entries;
    return entries.slice(options.offset, options.offset + options.count);
  }

  GetEntriesForExemplar(exemplar: string, options: QueryOptions | undefined): Promise<IEntry[]> {
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
