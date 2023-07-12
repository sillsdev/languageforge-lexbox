import { writable, readonly } from 'svelte/store';
export interface Notification {
  message: string;
  category: 'success' | 'warning';
  duration: number;
}

const _notifications = writable([]);
export const notifications = readonly(_notifications);
export function notifySuccess(
  message: string,
  duration = 4,
): void {

  const notification: Notification = {
    message,
    category: 'success',
    duration,
  };

  _notifications.update((currentNotifications) => [...currentNotifications, notification]);
  setTimeout(() => {
    removeNotification(notification);
  }, duration*1000);
}
export function notifyWarning(
  message: string,
  duration = 4,
): void {

  const notification: Notification = {
    message,
    category: 'alert-warning',
    duration,
  };

  _notifications.update((currentNotifications) => [...currentNotifications, notification]);
  setTimeout(() => {
    removeNotification(notification);
  }, duration*1000);
}
export function removeNotification(notification: Notification) {
  _notifications.update((currentNotifications) => currentNotifications.filter((n: Notification) => n !== notification));
}

export function removeAllNotifications() {
  _notifications.set([]);
}
