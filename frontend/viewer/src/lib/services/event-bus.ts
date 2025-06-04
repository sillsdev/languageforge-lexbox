import {DotnetService, type IEntry} from '$lib/dotnet-types';
import {useService} from '$lib/services/service-provider';
import type {IJsEventListener} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IJsEventListener';
import type {IFwEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IFwEvent';
import {FwEventType} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/FwEventType';
import type {IEntryChangedEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IEntryChangedEvent';
import type {IProjectEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IProjectEvent';
import type {IEntryDeletedEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IEntryDeletedEvent';
import {ProjectDataFormat} from '$lib/dotnet-types/generated-types/MiniLcm/Models/ProjectDataFormat';
import {type ProjectContext, useProjectContext} from '$lib/project-context.svelte';
import {onDestroy} from 'svelte';

export enum CloseReason {
  User = 0,
  Locked = 1,
}

export class EventBus {
  private _onEvent = new Set<(event: IFwEvent) => void>();
  private _onProjectClosed = new Set<(reason: CloseReason) => void>();
  constructor() {
    void this.eventLoop(useService(DotnetService.JsEventListener));
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
    this.notifyProjectEvent({entryId, type: FwEventType.EntryDeleted, isGlobal: false} satisfies IEntryDeletedEvent);
  }

  private notifyProjectEvent<T extends IFwEvent>(event: T) {
    this.eventBus.notifyEvent({
      type: FwEventType.ProjectEvent,
      isGlobal: true,
      project: {name: this.projectCode, dataFormat: ProjectDataFormat.Harmony},
      event: event
    } as IProjectEvent);
  }

  public onEntryUpdated(callback: (entry: IEntry) => void) {
    this.onProjectEvent(event => {
      if (isEntryChangedEvent(event)) {
        callback(event.entry);
      }
    });
  }
  public onEntryDeleted(callback: (entryId: string) => void) {
    this.onProjectEvent(event => {
      if (isEntryDeletedEvent(event)) {
        callback(event.entryId);
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

function isEntryChangedEvent(event: IFwEvent): event is IEntryChangedEvent {
  return event.type === FwEventType.EntryChanged;
}

function isEntryDeletedEvent(event: IFwEvent): event is IEntryDeletedEvent {
  return event.type === FwEventType.EntryDeleted;
}

function isProjectEvent(event: IFwEvent): event is IProjectEvent {
  return event.type === FwEventType.ProjectEvent;
}
