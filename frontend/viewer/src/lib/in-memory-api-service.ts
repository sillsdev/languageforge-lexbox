﻿/* eslint-disable @typescript-eslint/naming-convention */

import {
  DotnetService,
  type IComplexFormComponent,
  type IComplexFormType,
  type IEntry,
  type IExampleSentence,
  type IFilterQueryOptions,
  type IMiniLcmJsInvokable,
  type IPartOfSpeech,
  type IProjectModel,
  type IPublication,
  type IQueryOptions,
  type ISemanticDomain,
  type ISense,
  type IServerProjects,
  type IWritingSystem,
  type IWritingSystems,
  type WritingSystemType
} from '$lib/dotnet-types';
import {entries, partsOfSpeech, projectName, writingSystems} from './demo-entry-data';

import {WritingSystemService} from './writing-system-service.svelte';
import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
import {delay} from '$lib/utils/time';
import {initProjectContext, ProjectContext} from '$lib/project-context.svelte';
import type {IFwLiteConfig} from '$lib/dotnet-types/generated-types/FwLiteShared/IFwLiteConfig';
import type {IReadFileResponseJs} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IReadFileResponseJs';
import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
import type {ILcmFileMetadata} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ILcmFileMetadata';
import type {IUploadFileResponse} from '$lib/dotnet-types/generated-types/MiniLcm/Media/IUploadFileResponse';
import {UploadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/UploadFileResult';
import {DownloadProjectByCodeResult} from './dotnet-types/generated-types/FwLiteShared/Projects/DownloadProjectByCodeResult';

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

export const mockFwLiteConfig: IFwLiteConfig = {
  appVersion: `dev`,
  feedbackUrl: '',
  os: FwLitePlatform.Web,
  useDevAssets: true,
  edition: 0,
  updateCheckCondition: 0,
  updateCheckInterval: 0,
  updateUrl: ''
};

export class InMemoryApiService implements IMiniLcmJsInvokable {
  #writingSystemService: WritingSystemService;
  constructor(private projectContext: ProjectContext) {
    this.#writingSystemService = new WritingSystemService(projectContext);
  }
  countEntries(query?: string, options?: IFilterQueryOptions): Promise<number> {
    const entries = this.getFilteredEntries(query, options);
    return Promise.resolve(entries.length);
  }

  public static newProjectContext() {
    const projectContext = new ProjectContext();
    projectContext.setup({api: new InMemoryApiService(projectContext), projectName, projectCode: projectName});
    return projectContext;
  }
  public static setup(): InMemoryApiService {
    const projectContext = initProjectContext();
    const inMemoryLexboxApi = new InMemoryApiService(projectContext);
    projectContext.setup({api: inMemoryLexboxApi, projectName: inMemoryLexboxApi.projectName, projectCode: inMemoryLexboxApi.projectName})
    window.lexbox.ServiceProvider.setService(DotnetService.FwLiteConfig, mockFwLiteConfig);
    window.lexbox.ServiceProvider.setService(DotnetService.CombinedProjectsService, {
      localProjects(): Promise<IProjectModel[]> {
        return Promise.resolve([]);
      },
      supportsFwData: function (): Promise<boolean> {
        return Promise.resolve(false);
      },
      remoteProjects: function (): Promise<IServerProjects[]> {
        return Promise.resolve([]);
      },
      serverProjects: function (serverId: string, _forceRefresh: boolean): Promise<IServerProjects> {
        const server = {
          authority: '',
          displayName: '',
          id: serverId,
        }
        return Promise.resolve({server, projects: [], canDownloadByCode: false});
      },
      downloadProject: function (_project: IProjectModel): Promise<void> {
        return Promise.resolve();
      },
      downloadProjectByCode: function (_code, _server, _userRole): Promise<DownloadProjectByCodeResult> {
        return Promise.resolve(DownloadProjectByCodeResult.Success);
      },
      createProject: function (_name: string): Promise<void> {
        return Promise.resolve();
      },
      deleteProject: function (_code: string): Promise<void> {
        return Promise.resolve();
      }
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

  async getEntries(options: IQueryOptions | undefined): Promise<IEntry[]> {
    await delay(300);
    return this.queryEntries(undefined, options);
  }

  getWritingSystems(): Promise<IWritingSystems> {
    return Promise.resolve(writingSystems as unknown as IWritingSystems);
  }

  async searchEntries(query: string, options: IQueryOptions | undefined): Promise<IEntry[]> {
    await delay(300);
    return this.queryEntries(query, options);
  }

  private queryEntries(query: string | undefined, options: IQueryOptions | undefined): IEntry[] {
    const entries = this.getFilteredEntries(query, options);

    if (!options) return entries;
    const defaultWs = writingSystems.vernacular[0].wsId;
    const sortWs = pickWs(options.order.writingSystem, defaultWs);
    return entries
      .sort((e1, e2) => {
        const v1 = this.#writingSystemService.headword(e1, sortWs);
        const v2 = this.#writingSystemService.headword(e2, sortWs);
        if (!v2) return -1;
        if (!v1) return 1;
        let compare = v1.localeCompare(v2, sortWs);
        if (compare == 0) compare = e1.id.localeCompare(e2.id);
        return options.order.ascending ? compare : -compare;
      })
      .slice(options.offset, options.offset + options.count);
  }

  private getFilteredEntries(query?: string, options?: IFilterQueryOptions): IEntry[] {
    let entries = this._Entries();
    if (query) entries = filterEntries(entries, query);
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
    return entries;
  }

  async getEntry(guid: string) {
    const entry = this._entries.find(e => e.id === guid);
    await delay(300);
    if (!entry) throw new Error(`Entry ${guid} not found`);
    return JSON.parse(JSON.stringify(entry)) as IEntry;
  }

  createEntry(entry: IEntry): Promise<IEntry> {
    this._entries.push(entry);
    return Promise.resolve(entry);
  }

  updateEntry(_before: IEntry, after: IEntry): Promise<IEntry> {
    this._entries.splice(this._entries.findIndex(e => e.id === after.id), 1, after);
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
    this._entries.splice(this._entries.findIndex(e => e.id === guid), 1);
    return Promise.resolve();
  }

  deleteSense(entryGuid: string, senseGuid: string): Promise<void> {
    const entry = this._entries.find(e => e.id === entryGuid)!;
    entry.senses.splice(entry.senses.findIndex(s => s.id === senseGuid), 1);
    return Promise.resolve();
  }

  deleteExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string): Promise<void> {
    const entry = this._entries.find(e => e.id === entryGuid)!;
    const sense = entry.senses.find(s => s.id === senseGuid)!;
    sense.exampleSentences.splice(sense.exampleSentences.findIndex(es => es.id === exampleSentenceGuid), 1);
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
    const entry = this._entries.find(e => e.id == _entryId);
    if (!entry) throw new Error(`Entry ${_entryId} not found`);
    const index = entry.senses.findIndex(s => s.id == _before.id);
    if (index == -1) throw new Error(`Sense ${_before.id} not found`);
    entry.senses.splice(index, 1, _after);
    return Promise.resolve(_after);
  }

  addSemanticDomainToSense(_senseId: string, _semanticDomain: ISemanticDomain): Promise<void> {
    throw new Error('Method not implemented.');
  }

  removeSemanticDomainFromSense(_senseId: string, _semanticDomainId: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  updateExampleSentence(_entryId: string, _senseId: string, _before: IExampleSentence, _after: IExampleSentence): Promise<IExampleSentence> {
    const entry = this._entries.find(e => e.id == _entryId);
    if (!entry) throw new Error(`Entry ${_entryId} not found`);
    const sense = entry.senses.find(s => s.id == _senseId);
    if (!sense) throw new Error(`Sense ${_senseId} not found`);
    const index = sense.exampleSentences.findIndex(es => es.id == _before.id);
    if (index == -1) throw new Error(`ExampleSentence ${_before.id} not found`);
    sense.exampleSentences.splice(index, 1, _after);
    return Promise.resolve(_after);
  }

  getPublications(): Promise<IPublication[]> {
    throw new Error('Method not implemented.');
  }

  dispose(): Promise<void> {
    throw new Error('Method not implemented.');
  }

  getFileStream(_mediaUri: string): Promise<IReadFileResponseJs> {
    return Promise.resolve({result: ReadFileResult.NotSupported});
  }

  saveFile(_streamReference: Blob | ArrayBuffer | Uint8Array, _metadata: ILcmFileMetadata): Promise<IUploadFileResponse> {
    return Promise.resolve({result: UploadFileResult.NotSupported});
  }
}
