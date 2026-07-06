/* eslint-disable @typescript-eslint/naming-convention */

import {
  DotnetService,
  type IComplexFormComponent,
  type IVariant,
  type IVariantType,
  type IComplexFormType,
  type ICreateEntryOptions,
  type IEntry,
  type IExampleSentence,
  type IFilterQueryOptions,
  type IIndexQueryOptions,
  type IMiniLcmFeatures,
  type IMiniLcmJsInvokable,
  type IMorphType,
  type IPartOfSpeech,
  type IProjectModel,
  type IPublication,
  type IQueryOptions,
  type ISemanticDomain,
  type ISense,
  type IServerProjects,
  type IWritingSystem,
  type IWritingSystems,
  type WritingSystemType,
  type ICustomView,
  ViewBase,
  MorphTypeKind,
} from '$lib/dotnet-types';
import {entries, morphTypes, partsOfSpeech, projectName, variantTypes, writingSystems} from './demo-entry-data';

import {WritingSystemService} from '../data/writing-system-service.svelte';
import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
import {delay} from '$lib/utils/time';
import {initProjectContext, type ProjectContext} from '$project/project-context.svelte';
import type {IFwLiteConfig} from '$lib/dotnet-types/generated-types/FwLiteShared/IFwLiteConfig';
import type {IReadFileResponseJs} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IReadFileResponseJs';
import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
import type {ILcmFileMetadata} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ILcmFileMetadata';
import type {IUploadFileResponse} from '$lib/dotnet-types/generated-types/MiniLcm/Media/IUploadFileResponse';
import {UploadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/UploadFileResult';
import {DownloadProjectByCodeResult} from '$lib/dotnet-types/generated-types/FwLiteShared/Projects/DownloadProjectByCodeResult';
import type {IUpdateService} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IUpdateService';
import {type IAvailableUpdate, UpdateResult} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate';
import {type EventBus, useEventBus, ProjectEventBus} from '$lib/services/event-bus';
import type {IJsEventListener} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IJsEventListener';
import {initProjectStorage} from '$lib/storage';
import {MorphTypesService} from '$project/data/morph-types.svelte';
import type {ICommentThread} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ICommentThread';
import type {IUserComment} from '$lib/dotnet-types/generated-types/MiniLcm/Models/IUserComment';
import type {SubjectType} from '$lib/dotnet-types/generated-types/MiniLcm/Models/SubjectType';
import type {ThreadStatus} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ThreadStatus';

function pickWs(ws: string, defaultWs: string): string {
  return ws === 'default' ? defaultWs : ws;
}

const complexFormTypes = entries
  .flatMap(entry => entry.complexFormTypes)
  .filter((value, index, all) => all.findIndex(v2 => v2.id === value.id) === index);

export const mockFwLiteConfig: IFwLiteConfig = {
  appVersion: 'dev',
  feedbackUrl: '',
  os: FwLitePlatform.Web,
  useDevAssets: true,
  devAssetsPort: 5173,
  edition: 0,
  updateCheckCondition: 0,
  updateCheckInterval: 0,
  updateUrl: ''
};

export const mockUpdateService: IUpdateService = {
  checkForUpdates(): Promise<IAvailableUpdate | null> {
    return Promise.resolve({
      supportsAutoUpdate: true,
      release: {
        version: '1.0.1',
        url: 'https://lexbox.org/fw-lite',
      }
    });
  },
  async applyUpdate(_update: IAvailableUpdate): Promise<UpdateResult> {
    await delay(2000);
    return Promise.resolve(UpdateResult.Success);
  }
};

const mockJsEventListener: IJsEventListener = {
  nextEventAsync: () => Promise.resolve(null!),
  lastEvent: () => Promise.resolve(null)
};

export class InMemoryDemoApi implements IMiniLcmJsInvokable {
  #morphTypesService: MorphTypesService;
  #writingSystemService: WritingSystemService;
  #projectEventBus: ProjectEventBus;
  constructor(projectContext: ProjectContext, eventBus: EventBus) {
    this.#morphTypesService = new MorphTypesService(projectContext);
    this.#writingSystemService = new WritingSystemService(projectContext, this.#morphTypesService);
    this.#projectEventBus = new ProjectEventBus(projectContext, eventBus);

    // Trigger activating/loading lazy resources.
    // The fact that we need this "hack" might seem like lazy resources themselves are problematic,
    // but a MiniLCM API is supposed to be across a solid boundary and not indirectly relying on itself!
    // We're just doing it here for convenience and it's biting us a tad.
    void this.#morphTypesService.refetch();
    this.#writingSystemService.allWritingSystems();
  }

  public static setup(): InMemoryDemoApi {
    const projectContext = initProjectContext();
    const eventBus = useEventBus();
    const inMemoryLexboxApi = new InMemoryDemoApi(projectContext, eventBus);
    projectContext.setup({api: inMemoryLexboxApi, projectName: inMemoryLexboxApi.projectName, projectCode: inMemoryLexboxApi.projectName})
    initProjectStorage(projectContext.projectCode);
    window.lexbox.ServiceProvider.setService(DotnetService.FwLiteConfig, mockFwLiteConfig);
    window.lexbox.ServiceProvider.setService(DotnetService.UpdateService, mockUpdateService);
    window.lexbox.ServiceProvider.setService(DotnetService.JsEventListener, mockJsEventListener);
    window.__PLAYWRIGHT_UTILS__ = { demoApi: inMemoryLexboxApi };

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
      createProject: function (_name: string, _code: string, _vernacularWs: string, _analysisWs?: string): Promise<void> {
        return Promise.resolve();
      },
      createDemoProject: function (_name: string): Promise<void> {
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

  getMorphTypes(): Promise<IMorphType[]> {
    return Promise.resolve(
      morphTypes
      // [
      //     {id: 'd7f713e8-e8cf-11d3-9764-00c04f186933', kind: MorphTypeKind.Stem},
      //     {id: 'd7f713db-e8cf-11d3-9764-00c04f186933', kind: MorphTypeKind.Prefix, postfix='-'},
      //     {id: 'd7f713dd-e8cf-11d3-9764-00c04f186933', kind: MorphTypeKind.Suffix, prefix='-'},
      // ]
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
      audio: true,
      customViews: true,
    } satisfies IMiniLcmFeatures);
  }

  readonly projectName = projectName;

  private _commentThreads: ICommentThread[] = [];
  private _userComments: IUserComment[] = [];

  getCommentThreads(subjectType: SubjectType, subjectId: string, includeComments = false): Promise<ICommentThread[]> {
    return Promise.resolve(this._commentThreads
      .filter(thread => !thread.deletedAt && thread.subjectType === subjectType && thread.subjectId === subjectId)
      .map(thread => ({
        ...thread,
        comments: includeComments
          ? this._userComments.filter(comment => !comment.deletedAt && comment.commentThreadId === thread.id).map(comment => ({...comment}))
          : undefined,
      })));
  }

  getCommentThread(id: string): Promise<ICommentThread | null> {
    const thread = this._commentThreads.find(thread => thread.id === id && !thread.deletedAt);
    return Promise.resolve(thread ? {...thread} : null);
  }

  getUserComments(threadId: string): Promise<IUserComment[]> {
    return Promise.resolve(this._userComments
      .filter(comment => !comment.deletedAt && comment.commentThreadId === threadId)
      .map(comment => ({...comment})));
  }

  getUserComment(id: string): Promise<IUserComment | null> {
    const comment = this._userComments.find(comment => comment.id === id && !comment.deletedAt);
    return Promise.resolve(comment ? {...comment} : null);
  }

  getUnreadComments(_threadId?: string): Promise<IUserComment[]> {
    return Promise.resolve([]);
  }

  getUnreadCommentsForSubject(_subjectType: SubjectType, _subjectId: string): Promise<IUserComment[]> {
    return Promise.resolve([]);
  }

  countUnreadComments(_threadId?: string): Promise<number> {
    return Promise.resolve(0);
  }

  createCommentThread(thread: ICommentThread, firstComment: IUserComment): Promise<ICommentThread> {
    const now = new Date().toISOString();
    const createdThread = {
      ...thread,
      authorId: thread.authorId ?? firstComment.authorId,
      authorName: thread.authorName ?? firstComment.authorName,
      createdAt: thread.createdAt || now,
      updatedAt: thread.updatedAt || now,
    };
    const createdComment = {
      ...firstComment,
      commentThreadId: createdThread.id,
      createdAt: firstComment.createdAt || now,
      updatedAt: firstComment.updatedAt || now,
    };
    this._commentThreads = [...this._commentThreads, createdThread];
    this._userComments = [...this._userComments, createdComment];
    return Promise.resolve({...createdThread});
  }

  addUserComment(threadId: string, comment: IUserComment): Promise<IUserComment> {
    const now = new Date().toISOString();
    const createdComment = {
      ...comment,
      commentThreadId: threadId,
      createdAt: comment.createdAt || now,
      updatedAt: comment.updatedAt || now,
    };
    this._userComments = [...this._userComments, createdComment];
    this.touchCommentThread(threadId, now);
    return Promise.resolve({...createdComment});
  }

  editUserComment(commentId: string, text: string): Promise<IUserComment> {
    const now = new Date().toISOString();
    const index = this._userComments.findIndex(comment => comment.id === commentId);
    if (index === -1) throw new Error(`Comment ${commentId} not found`);

    const updatedComment = {...this._userComments[index], text, updatedAt: now};
    this._userComments = this._userComments.map((comment, i) => i === index ? updatedComment : comment);
    this.touchCommentThread(updatedComment.commentThreadId, now);
    return Promise.resolve({...updatedComment});
  }

  setCommentThreadStatus(threadId: string, status: ThreadStatus): Promise<ICommentThread> {
    const now = new Date().toISOString();
    const index = this._commentThreads.findIndex(thread => thread.id === threadId);
    if (index === -1) throw new Error(`Comment thread ${threadId} not found`);

    const updatedThread = {...this._commentThreads[index], status, updatedAt: now};
    this._commentThreads = this._commentThreads.map((thread, i) => i === index ? updatedThread : thread);
    return Promise.resolve({...updatedThread});
  }

  deleteUserComment(commentId: string): Promise<void> {
    const now = new Date().toISOString();
    this._userComments = this._userComments.map(comment => comment.id === commentId
      ? {...comment, deletedAt: now, updatedAt: now}
      : comment);
    return Promise.resolve();
  }

  deleteCommentThread(threadId: string): Promise<void> {
    const now = new Date().toISOString();
    this._commentThreads = this._commentThreads.map(thread => thread.id === threadId
      ? {...thread, deletedAt: now, updatedAt: now}
      : thread);
    this._userComments = this._userComments.map(comment => comment.commentThreadId === threadId
      ? {...comment, deletedAt: now, updatedAt: now}
      : comment);
    return Promise.resolve();
  }

  markCommentRead(_commentId: string): Promise<void> {
    return Promise.resolve();
  }

  markCommentThreadRead(_threadId: string): Promise<void> {
    return Promise.resolve();
  }

  markAllCommentsRead(): Promise<void> {
    return Promise.resolve();
  }

  private touchCommentThread(threadId: string, updatedAt: string): void {
    this._commentThreads = this._commentThreads.map(thread => thread.id === threadId ? {...thread, updatedAt} : thread);
  }

  private _customViews: ICustomView[] = [{
    id: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    name: 'Portuguese and audio',
    base: ViewBase.FwLite,
    entryFields: [
      {fieldId: 'lexemeForm'},
    ],
    senseFields: [
      {fieldId: 'gloss'},
    ],
    exampleFields: [
      {fieldId: 'sentence'},
      {fieldId: 'translations'},
    ],
    analysis: [{wsId: 'pt'}],
    vernacular: [{wsId: 'seh-Zxxx-x-audio'}],
  }];

  getCustomViews(): Promise<ICustomView[]> {
    return Promise.resolve(JSON.parse(JSON.stringify(this._customViews)) as ICustomView[]);
  }

  getCustomView(id: string): Promise<ICustomView | null> {
    const found = this._customViews.find(v => v.id === id) ?? null;
    return Promise.resolve(found ? (JSON.parse(JSON.stringify(found)) as ICustomView) : null);
  }

  private _entries = entries;

  private _Entries(): IEntry[] {
    return JSON.parse(JSON.stringify(this._entries)) as IEntry[];
  }

  async getEntries(options: IQueryOptions | undefined): Promise<IEntry[]> {
    await delay(300);
    return this.queryEntries(undefined, options);
  }

  countEntries(query?: string, options?: IFilterQueryOptions): Promise<number> {
    const entries = this.getFilteredEntries(query, options);
    return Promise.resolve(entries.length);
  }

  async getEntryIndex(entryId: string, query?: string, options?: IIndexQueryOptions): Promise<number> {
    await delay(100);
    const entries = this.getFilteredSortedEntries(query, options);
    return entries.findIndex(e => e.id === entryId);
  }

  getWritingSystems(): Promise<IWritingSystems> {
    return Promise.resolve(writingSystems as unknown as IWritingSystems);
  }

  async searchEntries(query: string, options: IQueryOptions | undefined): Promise<IEntry[]> {
    await delay(300);
    return this.queryEntries(query, options);
  }

  private queryEntries(query?: string, options?: IQueryOptions): IEntry[] {
    const entries = this.getFilteredSortedEntries(query, options);
    if (!options) return entries;
    return entries.slice(options.offset, options.offset + options.count);
  }

  private filterEntries(entries: IEntry[], query: string): IEntry[] {
    return entries.filter(entry =>
      [
        ...this.#writingSystemService.vernacular.map(ws => this.#writingSystemService.headword(entry, ws.wsId)),
        ...Object.values(entry.lexemeForm ?? {}),
        ...entry.senses.flatMap(sense => [
          ...Object.values(sense.gloss ?? {}),
        ]),
      ].some(value => value?.toLowerCase().includes(query.toLowerCase())));
  }

  private getFilteredSortedEntries(query?: string, options?: Omit<IQueryOptions, 'count' | 'offset'>): IEntry[] {
    const entries = this.getFilteredEntries(query, options);
    const defaultWs = writingSystems.vernacular[0].wsId;
    const sortWs = pickWs(options?.order?.writingSystem ?? defaultWs, defaultWs);
    const ascending = options?.order?.ascending ?? true;
    const stem = morphTypes.find(m => m.kind === MorphTypeKind.Stem)!;
    return entries
      .sort((e1, e2) => {
        // morph-tokens should not be included when sorting
        const v1 = e1.citationForm[sortWs] || e1.lexemeForm[sortWs];
        const v2 = e2.citationForm[sortWs] || e2.lexemeForm[sortWs];
        if (!v2) return -1;
        if (!v1) return 1;
        let compare = v1.localeCompare(v2, sortWs);
        if (compare === 0) {
          const m1 = (morphTypes.find(m => m.kind === e1.morphType) ?? stem);
          const m2 = (morphTypes.find(m => m.kind === e2.morphType) ?? stem);
          compare = m1.secondaryOrder - m2.secondaryOrder;
        }
        if (compare === 0) compare = e1.id.localeCompare(e2.id);
        return ascending ? compare : -compare;
      });
  }

  private getFilteredEntries(query?: string, options?: IFilterQueryOptions): IEntry[] {
    let entries = this._Entries();
    if (query) entries = this.filterEntries(entries, query);
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

  async createEntry(entry: IEntry, options: ICreateEntryOptions): Promise<IEntry> {
    if (options.autoAddMainPublication) {
      const main = (await this.getPublications()).find(p => p.isMain);
      if (main && !entry.publishIn.some(p => p.id === main.id)) entry.publishIn.push(main);
    }
    this._entries.push(entry);
    this.#projectEventBus.notifyEntryUpdated(entry);
    return entry;
  }

  updateEntry(_before: IEntry, after: IEntry): Promise<IEntry> {
    this._entries.splice(this._entries.findIndex(e => e.id === after.id), 1, after);
    this.#projectEventBus.notifyEntryUpdated(after);
    return Promise.resolve(after);
  }

  createSense(entryGuid: string, sense: ISense): Promise<ISense> {
    const entry = this._entries.find(e => e.id === entryGuid);
    if (!entry) throw new Error(`Entry ${entryGuid} not found`);
    entry.senses.push(sense);
    this.#projectEventBus.notifyEntryUpdated(entry);
    return Promise.resolve(sense);
  }

  createExampleSentence(entryGuid: string, senseGuid: string, exampleSentence: IExampleSentence): Promise<IExampleSentence> {
    const entry = this._entries.find(e => e.id === entryGuid);
    if (!entry) throw new Error(`Entry ${entryGuid} not found`);
    const sense = entry.senses.find(s => s.id === senseGuid);
    if (!sense) throw new Error(`Sense ${senseGuid} not found`);
    sense.exampleSentences.push(exampleSentence);
    this.#projectEventBus.notifyEntryUpdated(entry);
    return Promise.resolve(exampleSentence);
  }

  deleteEntry(guid: string): Promise<void> {
    const entryIndex = this._entries.findIndex(e => e.id === guid);
    if (entryIndex >= 0) {
      this._entries.splice(entryIndex, 1);
      this.#projectEventBus.notifyEntryDeleted(guid);
    }
    return Promise.resolve();
  }

  deleteSense(entryGuid: string, senseGuid: string): Promise<void> {
    const entry = this._entries.find(e => e.id === entryGuid)!;
    entry.senses.splice(entry.senses.findIndex(s => s.id === senseGuid), 1);
    this.#projectEventBus.notifyEntryUpdated(entry);
    return Promise.resolve();
  }

  deleteExampleSentence(entryGuid: string, senseGuid: string, exampleSentenceGuid: string): Promise<void> {
    const entry = this._entries.find(e => e.id === entryGuid)!;
    const sense = entry.senses.find(s => s.id === senseGuid)!;
    sense.exampleSentences.splice(sense.exampleSentences.findIndex(es => es.id === exampleSentenceGuid), 1);
    this.#projectEventBus.notifyEntryUpdated(entry);
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

  createCustomView(customView: ICustomView): Promise<ICustomView> {
    const created = JSON.parse(JSON.stringify(customView)) as ICustomView
    this._customViews = [...this._customViews, created];
    return Promise.resolve(created);
  }

  updateCustomView(_customView: ICustomView): Promise<ICustomView> {
    const index = this._customViews.findIndex(v => v.id === _customView.id);
    if (index === -1) throw new Error(`Custom view ${_customView.id} not found`);
    const updated = JSON.parse(JSON.stringify(_customView)) as ICustomView;
    this._customViews = this._customViews.map((v, i) => i === index ? updated : v);
    return Promise.resolve(updated);
  }

  deleteCustomView(_id: string): Promise<void> {
    this._customViews = this._customViews.filter(v => v.id !== _id);
    return Promise.resolve();
  }

  getVariantTypes(): Promise<IVariantType[]> {
    return Promise.resolve(variantTypes);
  }

  getVariantType(_id: string): Promise<IVariantType | null> {
    return Promise.resolve(variantTypes.find(vt => vt.id === _id) ?? null);
  }

  createVariant(_variant: IVariant): Promise<IVariant> {
    throw new Error('Method not implemented.');
  }

  deleteVariant(_variant: IVariant): Promise<void> {
    throw new Error('Method not implemented.');
  }

  addVariantType(_variant: IVariant, _variantTypeId: string): Promise<void> {
    throw new Error('Method not implemented.');
  }

  removeVariantType(_variant: IVariant, _variantTypeId: string): Promise<void> {
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
    this.#projectEventBus.notifyEntryUpdated(entry);
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
    this.#projectEventBus.notifyEntryUpdated(entry);
    return Promise.resolve(_after);
  }

  getPublications(): Promise<IPublication[]> {
    return Promise.resolve([
      {id: '1', name: {en: 'Main Dictionary'}, isMain: true, deletedAt: undefined},
      {id: '2', name: {en: 'School Dictionary'}, isMain: false, deletedAt: undefined},
    ]);
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
