import {DotnetService, type IEntry, ProjectDataFormat} from '$lib/dotnet-types';
import {useService} from '$lib/services/service-provider';
import type {IJsEventListener} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IJsEventListener';
import type {IFwEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IFwEvent';
import {FwEventType} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/FwEventType';
import type {IEntriesChangedEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IEntriesChangedEvent';
import type {IProjectEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IProjectEvent';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import {onDestroy} from 'svelte';
import type {ISyncEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/ISyncEvent';

export enum CloseReason {
  User = 0,
  Locked = 1,
}

interface OnEventOptions {
  includeLast?: boolean;
}

export class EventBus {
  private _onEvent = new Set<(event: IFwEvent) => void>();
  private _onProjectClosed = new Set<(reason: CloseReason) => void>();
  #jsEventListener: IJsEventListener;

  private _lastEventCache: Record<string, Partial<Record<FwEventType, IFwEvent>>> = {};
  constructor() {
    this.#jsEventListener = useService(DotnetService.JsEventListener);
    void this.eventLoop(this.#jsEventListener);
    this.onEvent(event => {
      if (isProjectEvent(event)) {
        this._lastEventCache[event.project.name] ??= {};
        this._lastEventCache[event.project.name][event.event.type] = event.event;
      }
    });
  }

  private async eventLoop(jsEventListener: IJsEventListener) {
    let event: IFwEvent;
    while (true) {
      event = await jsEventListener.nextEventAsync();
      if (!event) return;
      this.notifyEvent(event);
    }
  }

  public notifyEvent(event: IFwEvent) {
    //using set timeout to queue processing events outside the event loop, this prevents the event loop from being blocked
    setTimeout(() => {
      this._onEvent.forEach(callback => callback(event));
    });
  }

  public onProjectClosed(callback: (reason: CloseReason) => void): () => void {
    this._onProjectClosed.add(callback);
    return () => this._onProjectClosed.delete(callback);
  }

  public notifyProjectClosed(reason: CloseReason) {
    this._onProjectClosed.forEach(callback => callback(reason));
  }

  public onEvent(callback: (event: IFwEvent) => void): () => void {
    this._onEvent.add(callback);
    return () => this._onEvent.delete(callback);
  }

  public onEventType<T>(type: FwEventType, callback: (event: T) => void, options?: OnEventOptions) {
    if (options?.includeLast) {
      this.#jsEventListener.lastEvent(type).then(event => {
        if (!event) return;
        callback(event as T);
      }).catch(e => console.error('Error getting last event', e));
    }
    onDestroy(this.onEvent((event: IFwEvent) => {
      if (event.type === type) {
        callback(event as T);
      }
    }));
  }

  public getLastEvent<T extends IFwEvent>(projectCode: string, eventType: FwEventType): T | undefined {
    return this._lastEventCache[projectCode]?.[eventType] as T;
  }

  public notifyEntryUpdated(entry: IEntry) {
    console.error('notifyEntryUpdated, no longer supported', entry);
  }
}

export class ProjectEventBus {

  constructor(private projectContext: ProjectContext, private eventBus: EventBus) {
  }

  get projectCode() {
    return this.projectContext.projectCode;
  }

  public notifyEntryDeleted(entryId: string) {
    this.notifyProjectEvent({changedEntryIds: [], deletedEntryIds: [entryId], type: FwEventType.EntriesChanged, isGlobal: false} satisfies IEntriesChangedEvent);
  }

  public notifyEntryUpdated(entry: IEntry) {
    this.notifyProjectEvent({changedEntryIds: [entry.id], deletedEntryIds: [], type: FwEventType.EntriesChanged, isGlobal: false} satisfies IEntriesChangedEvent);
  }

  private notifyProjectEvent<T extends IFwEvent>(event: T) {
    this.eventBus.notifyEvent({
      type: FwEventType.ProjectEvent,
      isGlobal: true,
      project: {name: this.projectCode, dataFormat: ProjectDataFormat.Harmony},
      event: event
    } as IProjectEvent);
  }

  public onEntryUpdated(callback: (entryId: string) => void) {
    this.onProjectEvent(event => {
      if (isEntriesChangedEvent(event)) {
        event.changedEntryIds.forEach(callback);
      }
    });
  }

  public onEntryDeleted(callback: (entryId: string) => void) {
    this.onProjectEvent(event => {
      if (isEntriesChangedEvent(event)) {
        event.deletedEntryIds.forEach(callback);
      }
    });
  }

  // Bulk subscription: one callback per sync/edit, for consumers that re-query
  // the whole set rather than react per entry (the list, stats).
  public onEntriesChanged(callback: (event: IEntriesChangedEvent) => void) {
    this.onProjectEvent(event => {
      if (isEntriesChangedEvent(event)) {
        callback(event);
      }
    });
  }

  public onSync(callback: (event: ISyncEvent) => void) {
    const lastEvent = this.eventBus.getLastEvent<ISyncEvent>(this.projectCode, FwEventType.Sync);
    if (lastEvent) callback(lastEvent);
    this.onProjectEvent(event => {
      if (isSyncEvent(event)) {
        callback(event);
      }
    });
  }

  private onProjectEvent(callback: (event: IFwEvent) => void) {
    const onProjectEventCallback = (event: IFwEvent) => {
      if (isProjectEvent(event) && event.project.name === this.projectCode) {
        callback(event.event);
      }
    }
    onDestroy(this.eventBus.onEvent(onProjectEventCallback));
  }
}

let changeEventBus: EventBus | undefined = undefined;

export function useEventBus(): EventBus {
  return changeEventBus ??= new EventBus();
}

export function useProjectEventBus() {
  return new ProjectEventBus(useProjectContext(), useEventBus());
}

function isEntriesChangedEvent(event: IFwEvent): event is IEntriesChangedEvent {
  return event.type === FwEventType.EntriesChanged;
}

function isProjectEvent(event: IFwEvent): event is IProjectEvent {
  return event.type === FwEventType.ProjectEvent;
}

function isSyncEvent(event: IFwEvent): event is ISyncEvent {
  return event.type === FwEventType.Sync;
}
