import type {IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
import {getContext} from 'svelte';

export function useHistoryService() {
  const projectName = getContext<string>('project-name');
  return new HistoryService(projectName);
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
  changeName: string | undefined,
} & EntityType;

export class HistoryService {

  constructor(private projectName: string) {
  }

  async load(objectId: string) {
    const data = await fetch(`/api/history/${this.projectName}/${objectId}`)
      .then(res => res.json()) as HistoryItem[];
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
    const data = await fetch(`/api/history/${this.projectName}/snapshot/at/${history.timestamp}?entityId=${objectId}`).then(res => res.json()) as EntityType['entity'];
    if (this.isEntry(data)) {
      return {...history, entity: data, entityName: 'Entry'};
    }
    if (this.isSense(data)) {
      return {...history, entity: data, entityName: 'Sense'};
    }
    if (this.isExample(data)) {
      return {...history, entity: data, entityName: 'ExampleSentence'};
    }
    throw new Error('Unable to determine type of object ' + JSON.stringify(data));
  }

  isEntry(data: EntityType['entity']): data is IEntry {
    return !this.isSense(data) && !this.isExample(data);
  }
  isSense(data: EntityType['entity']): data is ISense {
    if (data === undefined) return false;
    return 'entryId' in data;
  }
  isExample(data: EntityType['entity']): data is IExampleSentence {
    if (data === undefined) return false;
    return 'senseId' in data;
  }
}
