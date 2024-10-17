import type {CloseReason} from '../generated-signalr-client/TypedSignalR.Client/Lexbox.ClientServer.Hubs';
import type {Entry} from '../mini-lcm';

export class EventBus {
  private _onEntryUpdated = new Set<(entry: Entry) => void>();
  private _onProjectClosed = new Set<(reason: CloseReason) => void>();

  public onProjectClosed(callback: (reason: CloseReason) => void): () => boolean {
    this._onProjectClosed.add(callback);
    return () => this._onProjectClosed.delete(callback);
  }

  public notifyProjectClosed(reason: CloseReason): void {
    this._onProjectClosed.forEach(callback => callback(reason));
  }

  public onEntryUpdated(callback: (entry: Entry) => void): () => boolean {
    this._onEntryUpdated.add(callback);
    return () => this._onEntryUpdated.delete(callback);
  }

  public notifyEntryUpdated(entry: Entry): void {
    this._onEntryUpdated.forEach(callback => callback(entry));
  }
}

const changeEventBus = new EventBus();

export function useEventBus(): EventBus {
  return changeEventBus;
}
