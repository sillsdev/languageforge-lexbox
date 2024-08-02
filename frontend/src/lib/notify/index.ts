import { writable, type Readable, type Writable } from 'svelte/store';

import { Duration } from '$lib/util/time';
import { defineContext } from '$lib/util/context';

export interface Notification {
  message: string;
  category?: 'alert-warning';
  duration: Duration;
}

export const { use: useNotifications, init: initNotificationService } =
  defineContext<NotificationService>(() => new NotificationService(writable([])));

export class NotificationService {

  get notifications(): Readable<Notification[]> {
    return this._notifications;
  }

  constructor(private readonly _notifications: Writable<Notification[]>) {
    // _notifications.set([{ message: 'Test notification', duration: 4 }, { message: 'Test notification', duration: 4 }])
  }

  notifySuccess = (message: string, duration?: number): void => {
    this.addNotification({ message, duration: duration ?? Duration.Default });
  }

  notifyWarning = (message: string, duration?: number): void => {
    this.addNotification({ message, duration: duration ?? Duration.Default, category: 'alert-warning' });
  }

  removeNotification = (notification: Notification): void => {
    this._notifications.update((currentNotifications) => currentNotifications.filter((n: Notification) => n !== notification));
  }

  removeAllNotifications = (): void => {
    this._notifications.set([]);
  }

  private addNotification(notification: Notification): void {
    this._notifications.update((currentNotifications) => [...currentNotifications, notification]);
    if (notification.duration !== Duration.Persistent) {
      setTimeout(() => {
        this.removeNotification(notification);
      }, notification.duration);
    }
  }
}
