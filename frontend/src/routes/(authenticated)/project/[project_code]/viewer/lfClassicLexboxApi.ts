/* eslint-disable @typescript-eslint/naming-convention */

import type {
  IComplexFormType,
  IEntry,
  IExampleSentence,
  ISense,
  LexboxApiClient,
  IPartOfSpeech,
  IQueryOptions,
  ISemanticDomain,
  IWritingSystem,
  WritingSystemType,
  IWritingSystems,
} from 'viewer/lexbox-api';

import {SEMANTIC_DOMAINS_EN} from './semantic-domains.en.generated-data';

function prepareEntriesForUi(entries: IEntry[]): void {
  entries.forEach(entry => {
    entry.senses.forEach(sense => {
      sense.semanticDomains.forEach(sd => {
        sd.id = sd.code;
      });
      // @ts-expect-error partOfSpeech is only included on the server for the viewer.
      sense.partOfSpeechId = sense.partOfSpeech as string;
    });
  });
}

function preparePartsOfSpeedForUi(partsOfSpeech: IPartOfSpeech[]): void {
  partsOfSpeech.forEach(pos => {
    pos.id = pos.name['__key'];
  });
}

export class LfClassicLexboxApi implements LexboxApiClient {
  constructor(private projectCode: string) {
  }

  async getWritingSystems(): Promise<IWritingSystems> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/writingSystems`);
    return (await result.json()) as IWritingSystems;
  }

  async getEntries(_options: IQueryOptions | undefined): Promise<IEntry[]> {
    //todo pass query options into query
    const result = await fetch(`/api/lfclassic/${this.projectCode}/entries${this.toQueryParams(_options)}`);
    const entries = (await result.json()) as IEntry[];
    prepareEntriesForUi(entries);
    return entries;
  }

  async searchEntries(_query: string, _options: IQueryOptions | undefined): Promise<IEntry[]> {
    //todo pass query options into query
    const result = await fetch(`/api/lfclassic/${this.projectCode}/entries/${encodeURIComponent(_query)}${this.toQueryParams(_options)}`);
    const entries = (await result.json()) as IEntry[];
    prepareEntriesForUi(entries);
    return entries;
  }

  private toQueryParams(options: IQueryOptions | undefined): string {

    if (!options) return '';
    /* eslint-disable @typescript-eslint/no-unsafe-assignment */
    const asc = options.order.ascending ?? true;
    const params = new URLSearchParams({
      SortField: options.order.field,
      SortWritingSystem: options.order.writingSystem as string,
      Ascending: asc ? 'true' : 'false',
      Count: options.count.toString(),
      Offset: options.offset.toString()
    });
    if (options.exemplar) {
      params.set('ExemplarValue', options.exemplar.value);
      params.set('ExemplarWritingSystem', options.exemplar.writingSystem as string);
    }
    /* eslint-enable @typescript-eslint/no-unsafe-assignment */
    return '?' + params.toString();
  }

  async getPartsOfSpeech(): Promise<IPartOfSpeech[]> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/parts-of-speech`);
    const partsOfSpeech = (await result.json()) as IPartOfSpeech[];
    preparePartsOfSpeedForUi(partsOfSpeech);
    return partsOfSpeech;
  }

  getSemanticDomains(): Promise<ISemanticDomain[]> {
    return Promise.resolve(SEMANTIC_DOMAINS_EN);
  }

  getComplexFormTypes(): Promise<IComplexFormType[]> {
    return Promise.resolve([]);
  }

  createWritingSystem(_type: WritingSystemType, _writingSystem: IWritingSystem): Promise<void> {
    throw new Error('Method not implemented.');
  }

  getEntry(_guid: string): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  createEntry(_entry: IEntry): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  updateEntry(_before: IEntry, _after: IEntry): Promise<IEntry> {
    throw new Error('Method not implemented.');
  }

  createSense(_entryGuid: string, _sense: ISense): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  createExampleSentence(_entryGuid: string, _senseGuid: string, _exampleSentence: IExampleSentence): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  deleteEntry(_guid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  deleteSense(_entryGuid: string, _senseGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  deleteExampleSentence(_entryGuid: string, _senseGuid: string, _exampleSentenceGuid: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

}
