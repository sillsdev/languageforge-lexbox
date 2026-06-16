import {
  ActivitySort,
  type IActivityAuthor,
  type IActivityChangeType,
  type IActivityQuery,
  type IChangeContext,
  type IEntry,
  type IExampleSentence,
  type IHistoryLineItem,
  type IProjectActivity,
  type ISense,
} from '$lib/dotnet-types';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import {isEntry, isExample, isSense} from '$lib/utils';

export function useHistoryService() {
  const projectContext = useProjectContext()
  return new HistoryService(projectContext);
}

type EntityType = {entity: IEntry, entityName: 'Entry'}
  | {entity: ISense, entityName: 'Sense'}
  | {entity: IExampleSentence, entityName: 'ExampleSentence'}
  | {entity: undefined, entityName: undefined};

export type HistoryItem = IHistoryLineItem & EntityType;

export type {IActivityQuery, IActivityAuthor, IActivityChangeType};

export class HistoryService {

  get historyApi(): IHistoryServiceJsInvokable | undefined {
    return this.projectContext.historyService;
  }

  constructor(private projectContext: ProjectContext) {
  }

  get loaded() {
    return Boolean(this.projectContext.historyService);
  }

  async load(objectId: string) {
    this.ensureLoaded();
    const data = await this.historyApi.getHistory(objectId);
    if (!Array.isArray(data)) {
      console.error('Invalid history data', data);
      return [];
    }
    // Reverse the history so that the most recent changes are at the top
    return data.toReversed();
  }

  async fetchSnapshot(history: HistoryItem, objectId: string): Promise<HistoryItem> {
    this.ensureLoaded();
    const data = (await this.historyApi.getObject(history.commitId, objectId)) as EntityType['entity'];
    if (isEntry(data)) {
      return {...history, entity: data, entityName: 'Entry'};
    }
    if (isSense(data)) {
      return {...history, entity: data, entityName: 'Sense'};
    }
    if (isExample(data)) {
      return {...history, entity: data, entityName: 'ExampleSentence'};
    }
    throw new Error('Unable to determine type of object ' + JSON.stringify(data));
  }

  async listActivityAuthors(): Promise<IActivityAuthor[]> {
    this.ensureLoaded();
    return await this.historyApi.listActivityAuthors();
  }

  async listActivityChangeTypes(): Promise<IActivityChangeType[]> {
    this.ensureLoaded();
    return await this.historyApi.listActivityChangeTypes();
  }

  async activity(skip: number, take: number, query?: IActivityQuery): Promise<IProjectActivity[]> {
    this.ensureLoaded();
    return await this.historyApi.projectActivity(
        skip,
        take, 
        query?.authorId, 
        query?.authorName,
        query?.excludeFieldWorks ?? false,
        query?.sort ?? ActivitySort.NewestFirst);
  }

  async loadChangeContext(commitId: string, changeIndex: number): Promise<IChangeContext> {
    this.ensureLoaded();
    return this.historyApi.loadChangeContext(commitId, changeIndex);
  }

  private ensureLoaded(): asserts this is {loaded: true, historyApi: IHistoryServiceJsInvokable} {
    if (!this.loaded) {
      throw new Error('HistoryService not loaded');
    }
  }
}
