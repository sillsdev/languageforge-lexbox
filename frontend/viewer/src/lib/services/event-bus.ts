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
    do {
      event = await jsEventListener.nextEventAsync();
      if (!event) return;
      this.distributor(event);
    } while (event);
  }

  private distributor(event: IFwEvent) {
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
    const onEventCallback = (event: IFwEvent) => {
      const projectEvent = event as IProjectEvent;
      if (projectEvent.project.name !== projectName) return;
      if (projectEvent.event.type !== FwEventType.EntryChanged) return;

      const entryChangedEvent = projectEvent.event as IEntryChangedEvent;
      callback(entryChangedEvent.entry);
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
  return changeEventBus ?? (changeEventBus = new EventBus());
}
