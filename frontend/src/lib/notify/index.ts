import { writable, readonly } from 'svelte/store';
export interface Notification {
  message: string;
  category: 'alert-success' | 'alert-warning' | ''; //still allowing the other colors
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
    category: '', //blank for gray notifications
    duration,
  };

  _notifications.update((currentNotifications) => [...currentNotifications, notification]);
  setTimeout(() => {
    removeNotification(notification);
  }, duration*1000);
}
export function notifyWarning( // in case we need them to be different colors in the future this is its own function
  message: string,
  duration = 4,
): void {
  notifySuccess(message, duration);
}
export function removeNotification(notification: Notification) {
  _notifications.update((currentNotifications) => currentNotifications.filter((n: Notification) => n !== notification));
}

export function removeAllNotifications() {
  _notifications.set([]);
}
