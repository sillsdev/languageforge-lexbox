/* eslint-disable @typescript-eslint/naming-convention */
import {
  type IEntry, type IExampleSentence, type ISense, type JsonPatch,
  type QueryOptions,
  type WritingSystems,
  type WritingSystemType,
  type WritingSystem,
  type LexboxApiClient,
  type LexboxApiFeatures,
  type PartOfSpeech,
  type SemanticDomain
} from 'viewer/lexbox-api';


export class LfClassicLexboxApi implements LexboxApiClient {
  constructor(private projectCode: string) {
  }

  SupportedFeatures(): LexboxApiFeatures {
    return {};
  }

  async GetWritingSystems(): Promise<WritingSystems> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/writingSystems`);
    return (await result.json()) as WritingSystems;
  }

  async GetEntries(_options: QueryOptions | undefined): Promise<IEntry[]> {
    //todo pass query options into query
    const result = await fetch(`/api/lfclassic/${this.projectCode}/entries${this.toQueryParams(_options)}`);
    return (await result.json()) as IEntry[];
  }

  async SearchEntries(_query: string, _options: QueryOptions | undefined): Promise<IEntry[]> {
    //todo pass query options into query
    const result = await fetch(`/api/lfclassic/${this.projectCode}/entries/${encodeURIComponent(_query)}${this.toQueryParams(_options)}`);
    return (await result.json()) as IEntry[];
  }

  private toQueryParams(options: QueryOptions | undefined): string {

    if (!options) return '';
    /* eslint-disable @typescript-eslint/no-unsafe-assignment */
    const asc = options.order.ascending ?? true;
    const params = new URLSearchParams({
      SortField: options.order.field,
      SortWritingSystem: options.order.writingSystem,
      Ascending: asc ? 'true' : 'false',
      Count: options.count.toString(),
      Offset: options.offset.toString()
    });
    if (options.exemplar) {
      params.set('ExemplarValue', options.exemplar.value);
      params.set('ExemplarWritingSystem', options.exemplar.writingSystem);
    }
    /* eslint-enable @typescript-eslint/no-unsafe-assignment */
    return '?' + params.toString();
  }

  async GetPartsOfSpeech(): Promise<PartOfSpeech[]> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/parts-of-speech`);
    return (await result.json()) as PartOfSpeech[];
  }

  GetSemanticDomains(): Promise<SemanticDomain[]> {
    return Promise.resolve([]);
  }

  CreateWritingSystem(_type: WritingSystemType, _writingSystem: WritingSystem): Promise<void> {
    throw new Error('Method not implemented.');
  }

  UpdateWritingSystem(_wsId: string, _type: WritingSystemType, _update: JsonPatch): Promise<WritingSystem> {
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
