import {writable, type Readable, type Writable} from 'svelte/store';

interface NotificationAction {
  label: string;
  callback: () => void;
}

export class AppNotification {
  private static _notifications: Writable<AppNotification[]> = writable([]);
  public static get notifications(): Readable<AppNotification[]> {
    return this._notifications;
  }

  public static display(message: string, type: 'success' | 'error' | 'info' | 'warning', timeout?: 'short' | 'long' | number, detail?: string) {
    const notification = new AppNotification(message, type, undefined, detail);
    this._notifications.update(notifications => [...notifications, notification]);
    if (!timeout || typeof timeout === 'number' && timeout <= 0) return;
    if (typeof timeout === 'string') {
      timeout = timeout === 'short' ? 5000 : 30000;
    }
    setTimeout(() => {
      this.remove(notification);
    }, timeout);
  }

  public static displayAction(message: string, type: 'success' | 'error' | 'info' | 'warning', action: NotificationAction) {
    const notification = new AppNotification(message, type, action);
    this._notifications.update(notifications => [...notifications, notification]);
  }

  public static remove(notification: AppNotification): void {
    this._notifications.update(notifications => notifications.filter(n => n !== notification));
  }

  public static clear(): void {
    this._notifications.set([]);
  }

  private constructor(public message: string, public type: 'success' | 'error' | 'info' | 'warning', public action?: NotificationAction, public detail?: string) {
  }
}
