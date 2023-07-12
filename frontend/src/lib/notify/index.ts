import { writable } from 'svelte/store';
export interface Notification {
  id: string;
  message: string;
  category: string;
  duration: number;
}

export const notifications = writable([]);

export function addNotification(
  message = 'Success',
  category = 'success',
  duration = 4,
): void {
  console.log(duration);
  const id: string = Math.random().toString(36).substr(2, 9);

  const notification: Notification = {
    id,
    message,
    category,
    duration,
  };

  notifications.update((currentNotifications) => [...currentNotifications, notification]);
  setTimeout(() => {
    removeNotification(id);
  }, duration*1000);
}

export function removeNotification(id: string) {
  notifications.update((currentNotifications) => currentNotifications.filter((n: Notification) => n.id !== id));
}

export function removeAllNotifications() {
  notifications.set([]);
}
