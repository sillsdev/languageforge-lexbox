import { readonly, writable } from 'svelte/store';

export interface Notification {
  message: string;
  category: 'alert-success' | 'alert-warning' | ''; // still allowing the other colors
  duration: number;
}

const DEFAULT_DURATION = 4;

const _notifications = writable<Notification[]>([]);
export const notifications = readonly(_notifications);

export function notifySuccess(
  message: string,
  duration = DEFAULT_DURATION,
): void {
  addNotification({ message, category: '', duration });
}

export function notifyWarning( // in case we need them to be different colors in the future this is its own function
  message: string,
  duration = DEFAULT_DURATION,
): void {
  notifySuccess(message, duration);
}

function addNotification(notification: Notification): void {
  _notifications.update((currentNotifications) => [...currentNotifications, notification]);
  setTimeout(() => {
    removeNotification(notification);
  }, notification.duration * 1000);
}

export function removeNotification(notification: Notification): void {
  _notifications.update((currentNotifications) => currentNotifications.filter((n: Notification) => n !== notification));
}

export function removeAllNotifications(): void {
  _notifications.set([]);
}
