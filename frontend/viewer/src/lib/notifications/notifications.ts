import {writable, type Writable} from 'svelte/store';

interface NotificationAction {
  label: string;
  callback: () => void;
}

export class AppNotification {
  private static _notifications: Writable<AppNotification[]> = writable([]);
  public static get notifications(): Writable<AppNotification[]> {
    return this._notifications;
  }

  public static display(message: string, type: 'success' | 'error' | 'info' | 'warning', timeout: 'short' | 'long' | number = 'short') {
    const notification = new AppNotification(message, type);
    this._notifications.update(notifications => [...notifications, notification]);
    if (timeout === -1) return;
    if (typeof timeout === 'string') {
      timeout = timeout === 'short' ? 5000 : 30000;
    }
    setTimeout(() => {
      this._notifications.update(notifications => notifications.filter(n => n !== notification));
    }, timeout);
  }

  public static displayAction(message: string, type: 'success' | 'error' | 'info' | 'warning', action: NotificationAction) {
    const notification = new AppNotification(message, type, action);
    this._notifications.update(notifications => [...notifications, notification]);
  }

  private constructor(public message: string, public type: 'success' | 'error' | 'info' | 'warning', public action?: NotificationAction) {
  }
}
