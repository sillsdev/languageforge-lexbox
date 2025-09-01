import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
import type {
  IHistoryServiceJsInvokable
} from '$lib/dotnet-types/generated-types/FwLiteShared/Services/IHistoryServiceJsInvokable';
import type {IProjectActivity} from '$lib/dotnet-types/generated-types/LcmCrdt/IProjectActivity';
import {type ProjectContext, useProjectContext} from '$lib/project-context.svelte';
import {isEntry, isEntry, isExample, isSense} from '$lib/utils';
import {isExample} from '$lib/utils';

export function useHistoryService() {
  const projectContext = useProjectContext()
  return new HistoryService(projectContext);
}

type EntityType = { entity: IEntry, entityName: 'Entry' } | { entity: ISense, entityName: 'Sense' } | {
  entity: IExampleSentence,
  entityName: 'ExampleSentence'
} | { entity: undefined, entityName: undefined };

export type HistoryItem = {
  commitId: string,
  timestamp: string,
  previousTimestamp?: string,
  snapshotId: string,
  changeIndex: number,
  changeName: string | undefined,
  authorName: string | undefined,
} & EntityType;

export class HistoryService {
  get historyApi(): IHistoryServiceJsInvokable | undefined {
    if (import.meta.env.DEV) {
      //randomly return undefined to test fallback
      if (Math.random() < 0.5) {
        return undefined;
      }
    }
    return this.projectContext.historyService;
  }
  constructor(private projectContext: ProjectContext) {
  }

  async load(objectId: string) {
    const data = await (this.historyApi?.getHistory(objectId) ?? fetch(`/api/history/${this.projectContext.projectName}/${objectId}`)
      .then(res => res.json())) as HistoryItem[];
    if (!Array.isArray(data)) {
      console.error('Invalid history data', data);
      return [];
    }
    for (let i = 0; i < data.length; i++) {
      const historyElement = data[i];
      historyElement.previousTimestamp = data[i + 1]?.timestamp;
    }
    // Reverse the history so that the most recent changes are at the top
    return data.toReversed();
  }

  async fetchSnapshot(history: HistoryItem, objectId: string): Promise<HistoryItem> {
    const data = (await this.historyApi?.getObject(history.commitId, objectId)
      ?? await fetch(`/api/history/${this.projectContext.projectName}/snapshot/commit/${history.commitId}?entityId=${objectId}`)
          .then(res => res.json())) as EntityType['entity'];
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

  async activity(projectName: string): Promise<IProjectActivity[]> {
    return await (this.historyApi?.projectActivity() ?? fetch(`/api/activity/${projectName}`).then(res => res.json())) as IProjectActivity[];
  }
}
