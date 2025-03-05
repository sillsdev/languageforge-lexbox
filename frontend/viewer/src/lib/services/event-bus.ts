import {type CloseReason} from '../generated-signalr-client/TypedSignalR.Client/Lexbox.ClientServer.Hubs';
import {DotnetService, type IEntry} from '$lib/dotnet-types';
import {useService} from '$lib/services/service-provider';
import type {IJsEventListener} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IJsEventListener';
import type {IFwEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IFwEvent';
import {FwEventType} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/FwEventType';
import type {IEntryChangedEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IEntryChangedEvent';
import type {IProjectEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IProjectEvent';

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
      this.distributor(event);
    }
  }

  private distributor(event: IFwEvent) {
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

  public onEntryUpdated(projectName: string, callback: (entry: IEntry) => void): () => void {
    // eslint-disable-next-line func-style
    const onEventCallback = (event: IFwEvent) => {
      if (isEventForProject(event, projectName) && isEntryChangedEvent(event.event)) {
        callback(event.event.entry);
      }
    };
    this._onEvent.add(onEventCallback);
    return () => this._onEvent.delete(onEventCallback);
  }

  public notifyEntryUpdated(entry: IEntry) {
    console.error('notifyEntryUpdated, no longer supported', entry);
  }
}

let changeEventBus: EventBus | undefined = undefined;

export function useEventBus(): EventBus {
  return changeEventBus ??= new EventBus();
}

function isEntryChangedEvent(event: IFwEvent): event is IEntryChangedEvent {
  return event.type === FwEventType.EntryChanged;
}

function isProjectEvent(event: IFwEvent): event is IProjectEvent {
  return event.type === FwEventType.ProjectEvent;
}

function isEventForProject(event: IFwEvent, projectName: string): event is IProjectEvent {
  return isProjectEvent(event) && event.project.name === projectName;
}
