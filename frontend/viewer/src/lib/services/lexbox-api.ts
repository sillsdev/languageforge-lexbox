export type { IEntry, IExampleSentence, ISense, QueryOptions, WritingSystems } from '../mini-lcm';
import type { IEntry, IExampleSentence, ISense, QueryOptions, WritingSystems } from '../mini-lcm';
import type { Operation } from 'fast-json-patch';
export type JsonPatch = Operation[];

export interface LexboxApi {
  GetWritingSystems(): Promise<WritingSystems>;
  GetExemplars(): Promise<string[]>;

  GetEntriesForExemplar(exemplar: string, options: QueryOptions | undefined): Promise<IEntry[]>;
  GetEntries(options: QueryOptions | undefined): Promise<IEntry[]>;
  SearchEntries(query: string, options: QueryOptions | undefined): Promise<IEntry[]>;
  GetEntry(guid: string): Promise<IEntry>;

  CreateEntry(entry: IEntry): Promise<IEntry>;
  UpdateEntry(guid: string, update: JsonPatch): Promise<IEntry>;
  DeleteEntry(guid: string): Promise<void>;

  CreateSense(entryGuid: string, sense: ISense): Promise<ISense>;
  UpdateSense(entryGuid: string, senseGuid: string, update: JsonPatch): Promise<ISense>;
  DeleteSense(entryGuid: string, senseGuid: string): Promise<void>;

  CreateExampleSentence(entryGuid: string, senseGuid: string, exampleSentence: IExampleSentence): Promise<IExampleSentence>;
  UpdateExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string, update: JsonPatch): Promise<IExampleSentence>;
  DeleteExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string): Promise<void>;
}
