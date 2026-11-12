import {
  ActivitySort,
  type IActivityAuthor,
  type IActivityChangeType,
  type IActivityQuery,
  type IChangeContext,
  type IProjectActivity,
} from '$lib/dotnet-types';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';

export function useHistoryService() {
  const projectContext = useProjectContext()
  return new HistoryService(projectContext);
}

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
        query?.authorFilterKeys,
        query?.changeTypeKeys,
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
