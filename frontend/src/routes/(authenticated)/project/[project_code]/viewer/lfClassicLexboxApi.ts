/* eslint-disable @typescript-eslint/naming-convention */
import {
  type IEntry, type IExampleSentence, type ISense, type JsonPatch,
  type LexboxApi,
  type QueryOptions,
  type WritingSystems,
  type WritingSystemType,
  type WritingSystem
} from 'viewer/lexbox-api';


export class LfClassicLexboxApi implements LexboxApi {
  constructor(private projectCode: string) {
  }

  async GetWritingSystems(): Promise<WritingSystems> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/writingSystems`);
    return (await result.json()) as WritingSystems;
  }

  async GetEntries(_options: QueryOptions | undefined): Promise<IEntry[]> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/entries?order=desc&count=100`);
    return (await result.json()) as IEntry[];
  }

  CreateWritingSystem(_type: WritingSystemType, _writingSystem: WritingSystem): Promise<void> {
    throw new Error('Method not implemented.');
  }

  UpdateWritingSystem(_wsId: string, _type: WritingSystemType, _update: JsonPatch): Promise<WritingSystem> {
    throw new Error('Method not implemented.');
  }

  SearchEntries(_query: string, _options: QueryOptions | undefined): Promise<IEntry[]> {
    throw new Error('Method not implemented.');
  }

  GetEntry(_guid: string): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  CreateEntry(_entry: IEntry): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  UpdateEntry(_guid: string, _update: JsonPatch): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  CreateSense(_entryGuid: string, _sense: ISense): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  UpdateSense(_entryGuid: string, _senseGuid: string, _update: JsonPatch): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  CreateExampleSentence(_entryGuid: string, _senseGuid: string, _exampleSentence: IExampleSentence): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  UpdateExampleSentence(_entryGuid: string, _senseGuid: string, _exampleSentenceGuid: string, _update: JsonPatch): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  GetExemplars(): Promise<string[]> {
    throw new Error('Method not implemented.');
  }

  DeleteEntry(_guid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  DeleteSense(_entryGuid: string, _senseGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  DeleteExampleSentence(_entryGuid: string, _senseGuid: string, _exampleSentenceGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

}
