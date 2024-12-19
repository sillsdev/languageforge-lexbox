import type {CloseReason} from '../generated-signalr-client/TypedSignalR.Client/Lexbox.ClientServer.Hubs';
import type {IEntry} from '$lib/dotnet-types';

export class EventBus {
  private _onEntryUpdated = new Set<(entry: IEntry) => void>();
  private _onProjectClosed = new Set<(reason: CloseReason) => void>();

  public onProjectClosed(callback: (reason: CloseReason) => void): () => void {
    this._onProjectClosed.add(callback);
    return () => this._onProjectClosed.delete(callback);
  }

  public notifyProjectClosed(reason: CloseReason) {
    this._onProjectClosed.forEach(callback => callback(reason));
  }

  public onEntryUpdated(callback: (entry: IEntry) => void): () => void {
    this._onEntryUpdated.add(callback);
    return () => this._onEntryUpdated.delete(callback);
  }

  public notifyEntryUpdated(entry: IEntry) {
    this._onEntryUpdated.forEach(callback => callback(entry));
  }
}

const changeEventBus = new EventBus();

export function useEventBus(): EventBus {
  return changeEventBus;
}
