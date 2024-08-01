import { writable, type Readable, type Writable } from 'svelte/store';

import { Duration } from '$lib/util/time';
import { defineContext } from '$lib/util/context';

export interface Notification {
  message: string;
  category?: 'alert-warning';
  duration: number;
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

  notifySuccess = (message: string, duration = Duration.Default): void => {
    this.addNotification({ message, duration });
  }

  notifyWarning = (message: string, duration = Duration.Default): void => {
    this.addNotification({ message, duration, category: 'alert-warning' });
  }

  notifyPersistent = (message: string, category?: 'alert-warning'): void => {
    this.addPersistentNotification({ message, duration: Infinity, category });
  }

  removeNotification = (notification: Notification): void => {
    this._notifications.update((currentNotifications) => currentNotifications.filter((n: Notification) => n !== notification));
  }

  removeAllNotifications = (): void => {
    this._notifications.set([]);
  }

  private addPersistentNotification(notification: Notification): void {
    this._notifications.update((currentNotifications) => [...currentNotifications, notification]);
  }

  private addNotification(notification: Notification): void {
    this._notifications.update((currentNotifications) => [...currentNotifications, notification]);
    setTimeout(() => {
      this.removeNotification(notification);
    }, notification.duration);
  }
}
