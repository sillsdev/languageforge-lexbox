import { readonly, writable } from 'svelte/store';

export interface Notification {
  message: string;
  category?: 'alert-warning';
  duration: number;
}

export const enum Duration {
  Default = 5000,
  Medium = 10000,
  Long = 15000,
}

const _notifications = writable<Notification[]>([]);
// _notifications.set([{ message: 'Test notification', duration: 4 }, { message: 'Test notification', duration: 4 }])

export const notifications = readonly(_notifications);

export function notifySuccess(
  message: string,
  duration = Duration.Default,
): void {
  addNotification({ message, duration });
}

export function notifyWarning( // in case we need them to be different colors in the future this is its own function
  message: string,
  duration = Duration.Default,
): void {
  addNotification({ message, duration, category: 'alert-warning' });
}

function addNotification(notification: Notification): void {
  _notifications.update((currentNotifications) => [...currentNotifications, notification]);
  setTimeout(() => {
    removeNotification(notification);
  }, notification.duration);
}

export function removeNotification(notification: Notification): void {
  _notifications.update((currentNotifications) => currentNotifications.filter((n: Notification) => n !== notification));
}

export function removeAllNotifications(): void {
  _notifications.set([]);
}
