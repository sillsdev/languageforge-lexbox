import { writable } from 'svelte/store';
export interface Notification {
  message: string;
  category: string;
  duration: number;
}

export const notifications = writable([]);

export function addNotification(
  message: string,
  category = 'success',
  duration = 10,
): void {
  console.log(duration);

  const notification: Notification = {
    message,
    category,
    duration,
  };

  notifications.update((currentNotifications) => [...currentNotifications, notification]);
  setTimeout(() => {
    removeNotification(notification);
  }, duration*1000);
}

export function removeNotification(notification: Notification) {
  notifications.update((currentNotifications) => currentNotifications.filter((n: Notification) => n !== notification));
}

export function removeAllNotifications() {
  notifications.set([]);
}
