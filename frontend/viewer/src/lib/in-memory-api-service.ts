/* eslint-disable @typescript-eslint/naming-convention */

import {
  DotnetService,
  type IComplexFormComponent,
  type IComplexFormType,
  type IEntry,
  type IExampleSentence,
  type IMiniLcmJsInvokable,
  type IPartOfSpeech,
  type IQueryOptions,
  type ISemanticDomain,
  type ISense,
  type IWritingSystem,
  type IWritingSystems,
  type WritingSystemType
} from '$lib/dotnet-types';
import {entries, partsOfSpeech, projectName, writingSystems} from './demo-entry-data';

import {WritingSystemService} from './writing-system-service';
import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
import type {IPublication} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IPublication';

function pickWs(ws: string, defaultWs: string): string {
  return ws === 'default' ? defaultWs : ws;
}

const complexFormTypes = entries
  .flatMap(entry => entry.complexFormTypes)
  .filter((value, index, all) => all.findIndex(v2 => v2.id === value.id) === index);

function filterEntries(entries: IEntry[], query: string): IEntry[] {
  return entries.filter(entry =>
    [
      ...Object.values(entry.lexemeForm ?? {}),
      ...Object.values(entry.citationForm ?? {}),
      ...entry.senses.flatMap(sense => [
        ...Object.values(sense.gloss ?? {}),
      ]),
    ].some(value => value?.toLowerCase().includes(query.toLowerCase())));
}

const writingSystemService = new WritingSystemService(writingSystems);

export class InMemoryApiService implements IMiniLcmJsInvokable {

  public static setup(): InMemoryApiService {
    const inMemoryLexboxApi = new InMemoryApiService();
    window.lexbox.ServiceProvider.setService(DotnetService.MiniLcmApi, inMemoryLexboxApi);
    window.lexbox.ServiceProvider.setService(DotnetService.FwLiteConfig, {
      appVersion: 'test-project',
      feedbackUrl: '',
      os: FwLitePlatform.Web,
      useDevAssets: true,
    });
    return inMemoryLexboxApi;
  }

  getComplexFormTypes(): Promise<IComplexFormType[]> {
    return Promise.resolve(
      //*
      complexFormTypes
      /*/
      [
        {id: '13', name: {en: 'Compound'},},
        {id: '15', name: {en: 'Idiom'},}
      ]
      //*/
    );
  }

  getPartsOfSpeech(): Promise<IPartOfSpeech[]> {
    return Promise.resolve(
      partsOfSpeech
      // [
      //   {id: '86ff66f6-0774-407a-a0dc-3eeaf873daf7', name: {en: 'Verb'}, predefined: true},
      //   {id: 'a8e41fd3-e343-4c7c-aa05-01ea3dd5cfb5', name: {en: 'Noun'}, predefined: true}
      // ]
    );
  }

  getSemanticDomains(): Promise<ISemanticDomain[]> {
    return Promise.resolve([
      {id: '36e8f1df-1798-4ae6-904d-600ca6eb4145', name: {en: 'Fruit'}, code: '1', predefined: false},
      {id: 'Animal', name: {en: 'Animal'}, code: '2', predefined: false},
    ]);
  }

  supportedFeatures() {
    return Promise.resolve({
      write: true,
    });
  }

  readonly projectName = projectName;

  private _entries = entries;

  private _Entries(): IEntry[] {
    return JSON.parse(JSON.stringify(this._entries)) as IEntry[];
  }

  getEntries(options: IQueryOptions | undefined): Promise<IEntry[]> {
    return Promise.resolve(this.ApplyQueryOptions(this._Entries(), options));
  }

  getWritingSystemsSync(): IWritingSystems {
    return writingSystems;
  }
  getWritingSystems(): Promise<IWritingSystems> {
    return Promise.resolve(writingSystems);
  }

  searchEntries(query: string, options: IQueryOptions | undefined): Promise<IEntry[]> {
    return Promise.resolve(this.ApplyQueryOptions(filterEntries(this._Entries(), query), options));
  }

  private ApplyQueryOptions(entries: IEntry[], options: IQueryOptions | undefined): IEntry[] {
    if (!options) return entries;
    const defaultWs = writingSystems.vernacular[0].wsId;
    if (options.exemplar?.value) {
      const lowerExemplar = options.exemplar.value.toLowerCase();
      const exemplarWs = pickWs(options.exemplar.writingSystem, defaultWs);
      entries = entries.filter(entry =>
        (entry.citationForm[exemplarWs] ?? entry.lexemeForm[exemplarWs] ?? '')
          ?.toLocaleLowerCase()
          ?.startsWith(lowerExemplar));
    }

    const sortWs = pickWs(options.order.writingSystem, defaultWs);
    return entries
      .sort((e1, e2) => {
        const v1 = writingSystemService.headword(e1, sortWs);
        const v2 = writingSystemService.headword(e2, sortWs);
        if (!v2) return -1;
        if (!v1) return 1;
        const compare = v1.localeCompare(v2, sortWs);
        if (compare !== 0) return compare;
        return e1.id.localeCompare(e2.id);
      })
      .slice(options.offset, options.offset + options.count);
  }

  getEntry(guid: string) {
    const entry = entries.find(e => e.id === guid);
    if (!entry) throw new Error(`Entry ${guid} not found`);
    return Promise.resolve(entry);
  }

  createEntry(entry: IEntry): Promise<IEntry> {
    this._entries.push(entry);
    return Promise.resolve(entry);
  }

  updateEntry(_before: IEntry, after: IEntry): Promise<IEntry> {
    entries.splice(entries.findIndex(e => e.id === after.id), 1, after);
    return Promise.resolve(after);
  }

  createSense(entryGuid: string, sense: ISense): Promise<ISense> {
    this._entries.find(e => e.id === entryGuid)?.senses.push(sense);
    return Promise.resolve(sense);
  }

  createExampleSentence(entryGuid: string, senseGuid: string, exampleSentence: IExampleSentence): Promise<IExampleSentence> {
    this._entries.find(e => e.id === entryGuid)?.senses.find(s => s.id === senseGuid)?.exampleSentences.push(exampleSentence);
    return Promise.resolve(exampleSentence);
  }

  deleteEntry(guid: string): Promise<void> {
    entries.slice(entries.findIndex(e => e.id === guid), 1);
    return Promise.resolve();
  }

  deleteSense(entryGuid: string, senseGuid: string): Promise<void> {
    const entry = this._entries.find(e => e.id === entryGuid)!;
    entry.senses.slice(entry.senses.findIndex(s => s.id === senseGuid), 1);
    return Promise.resolve();
  }

  deleteExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string): Promise<void> {
    const entry = this._entries.find(e => e.id === entryGuid)!;
    const sense = entry.senses.find(s => s.id === senseGuid)!;
    sense.exampleSentences.slice(sense.exampleSentences.findIndex(es => es.id === exampleSentenceGuid), 1);
    return Promise.resolve();
  }

  createWritingSystem(_type: WritingSystemType, _writingSystem: IWritingSystem): Promise<IWritingSystem> {
    throw new Error('Method not implemented.');
  }

  getComplexFormType(_id: string): Promise<IComplexFormType> {
    throw new Error('Method not implemented.');
  }

  getSense(_entryId: string, _id: string): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  getPartOfSpeech(_id: string): Promise<IPartOfSpeech> {
    throw new Error('Method not implemented.');
  }

  getSemanticDomain(_id: string): Promise<ISemanticDomain> {
    throw new Error('Method not implemented.');
  }

  getExampleSentence(_entryId: string, _senseId: string, _id: string): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  updateWritingSystem(_before: IWritingSystem, _after: IWritingSystem): Promise<IWritingSystem> {
    throw new Error('Method not implemented.');
  }

  createPartOfSpeech(_partOfSpeech: IPartOfSpeech): Promise<IPartOfSpeech> {
    throw new Error('Method not implemented.');
  }

  updatePartOfSpeech(_before: IPartOfSpeech, _after: IPartOfSpeech): Promise<IPartOfSpeech> {
    throw new Error('Method not implemented.');
  }

  deletePartOfSpeech(_id: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  createSemanticDomain(_semanticDomain: ISemanticDomain): Promise<ISemanticDomain> {
    throw new Error('Method not implemented.');
  }

  updateSemanticDomain(_before: ISemanticDomain, _after: ISemanticDomain): Promise<ISemanticDomain> {
    throw new Error('Method not implemented.');
  }

  deleteSemanticDomain(_id: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  createComplexFormType(_complexFormType: IComplexFormType): Promise<IComplexFormType> {
    throw new Error('Method not implemented.');
  }

  updateComplexFormType(_before: IComplexFormType, _after: IComplexFormType): Promise<IComplexFormType> {
    throw new Error('Method not implemented.');
  }

  deleteComplexFormType(_id: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  createComplexFormComponent(_complexFormComponent: IComplexFormComponent): Promise<IComplexFormComponent> {
    throw new Error('Method not implemented.');
  }

  deleteComplexFormComponent(_complexFormComponent: IComplexFormComponent): Promise<void> {
    throw new Error('Method not implemented.');
  }

  addComplexFormType(_entryId: string, _complexFormTypeId: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  removeComplexFormType(_entryId: string, _complexFormTypeId: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  updateSense(_entryId: string, _before: ISense, _after: ISense): Promise<ISense> {
    throw new Error('Method not implemented.');
  }

  addSemanticDomainToSense(_senseId: string, _semanticDomain: ISemanticDomain): Promise<void> {
    throw new Error('Method not implemented.');
  }

  removeSemanticDomainFromSense(_senseId: string, _semanticDomainId: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  updateExampleSentence(_entryId: string, _senseId: string, _before: IExampleSentence, _after: IExampleSentence): Promise<IExampleSentence> {
    throw new Error('Method not implemented.');
  }

  getPublications(): Promise<IPublication[]> {
    throw new Error('Method not implemented.');
  }

  dispose(): Promise<void> {
    throw new Error('Method not implemented.');
  }

}
