/* eslint-disable @typescript-eslint/naming-convention,@typescript-eslint/no-unused-vars */

import type {
  IComplexFormType,
  IEntry,
  IExampleSentence,
  ISense,
  IMiniLcmJsInvokable,
  IPartOfSpeech,
  IQueryOptions,
  ISemanticDomain,
  IWritingSystem,
  WritingSystemType,
  IWritingSystems,
  IComplexFormComponent,
  IMiniLcmFeatures,
} from 'viewer/mini-lcm-api';

import {SEMANTIC_DOMAINS_EN} from './semantic-domains.en.generated-data';

function prepareEntriesForUi(entries: IEntry[]): void {
  for (const entry of entries) {
    for (const sense of entry.senses) {
      for (const sd of sense.semanticDomains) {
        sd.id = sd.code;
      }
      //partOfSpeech is only included on the server for the viewer.
      sense.partOfSpeechId = sense.partOfSpeech?.id;
    }
  }
}

function preparePartsOfSpeechForUi(partsOfSpeech: IPartOfSpeech[]): void {
  partsOfSpeech.forEach(pos => {
    pos.id = pos.name['__key'];
  });
}

export class LfClassicLexboxApi implements IMiniLcmJsInvokable {
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
    return '?' + params.toString();
  }

  async getPartsOfSpeech(): Promise<IPartOfSpeech[]> {
    const result = await fetch(`/api/lfclassic/${this.projectCode}/parts-of-speech`);
    const partsOfSpeech = (await result.json()) as IPartOfSpeech[];
    preparePartsOfSpeechForUi(partsOfSpeech);
    return partsOfSpeech;
  }

  getSemanticDomains(): Promise<ISemanticDomain[]> {
    return Promise.resolve(SEMANTIC_DOMAINS_EN.map(sd => ({...sd, predefined: false})));
  }

  getComplexFormTypes(): Promise<IComplexFormType[]> {
    return Promise.resolve([]);
  }

  createWritingSystem(_type: WritingSystemType, _writingSystem: IWritingSystem): Promise<IWritingSystem> {
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

  supportedFeatures(): Promise<IMiniLcmFeatures> {
    return Promise.resolve({

    });
  }

  getComplexFormType(id: string): Promise<IComplexFormType> {
    throw new Error('Method not implemented.');
  }

  getSense(entryId: string, id: string): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  getPartOfSpeech(id: string): Promise<IPartOfSpeech> {
    throw new Error('Method not implemented.');
  }

  getSemanticDomain(id: string): Promise<ISemanticDomain> {
    throw new Error('Method not implemented.');
  }

  getExampleSentence(entryId: string, senseId: string, id: string): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  updateWritingSystem(before: IWritingSystem, after: IWritingSystem): Promise<IWritingSystem> {
    throw new Error('Method not implemented.');
  }

  createPartOfSpeech(partOfSpeech: IPartOfSpeech): Promise<IPartOfSpeech> {
    throw new Error('Method not implemented.');
  }

  updatePartOfSpeech(before: IPartOfSpeech, after: IPartOfSpeech): Promise<IPartOfSpeech> {
    throw new Error('Method not implemented.');
  }

  deletePartOfSpeech(id: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  createSemanticDomain(semanticDomain: ISemanticDomain): Promise<ISemanticDomain> {
    throw new Error('Method not implemented.');
  }

  updateSemanticDomain(before: ISemanticDomain, after: ISemanticDomain): Promise<ISemanticDomain> {
    throw new Error('Method not implemented.');
  }

  deleteSemanticDomain(id: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  createComplexFormType(complexFormType: IComplexFormType): Promise<IComplexFormType> {
    throw new Error('Method not implemented.');
  }

  updateComplexFormType(before: IComplexFormType, after: IComplexFormType): Promise<IComplexFormType> {
    throw new Error('Method not implemented.');
  }

  deleteComplexFormType(id: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  createComplexFormComponent(complexFormComponent: IComplexFormComponent): Promise<IComplexFormComponent> {
    throw new Error('Method not implemented.');
  }

  deleteComplexFormComponent(complexFormComponent: IComplexFormComponent): Promise<void> {
    throw new Error('Method not implemented.');
  }

  addComplexFormType(entryId: string, complexFormTypeId: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  removeComplexFormType(entryId: string, complexFormTypeId: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  updateSense(entryId: string, before: ISense, after: ISense): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  addSemanticDomainToSense(senseId: string, semanticDomain: ISemanticDomain): Promise<void> {
    throw new Error('Method not implemented.');
  }

  removeSemanticDomainFromSense(senseId: string, semanticDomainId: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  updateExampleSentence(entryId: string, senseId: string, before: IExampleSentence, after: IExampleSentence): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  dispose(): Promise<void> {
    throw new Error('Method not implemented.');
  }
}
