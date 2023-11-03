import type { Readable, Writable } from 'svelte/store';

import { Duration } from '$lib/util/time';
import { defineContext } from '$lib/util/context';

export interface Notification {
  message: string;
  category?: 'alert-warning';
  duration: number;
}

export const { use: useNotifications, init: initNotificationService } = defineContext<NotificationService>();

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

  removeNotification = (notification: Notification): void => {
    this._notifications.update((currentNotifications) => currentNotifications.filter((n: Notification) => n !== notification));
  }

  removeAllNotifications = (): void => {
    this._notifications.set([]);
  }

  private addNotification(notification: Notification): void {
    this._notifications.update((currentNotifications) => [...currentNotifications, notification]);
    setTimeout(() => {
      this.removeNotification(notification);
    }, notification.duration);
  }
}
