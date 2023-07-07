<script lang="ts">
  import { writable, type Writable } from 'svelte/store';

  interface Notification {
    id: string;
    message: string;
    category: string;
    duration: number;
  }

  // Retrieve notifications from localStorage or use an empty array
  const initialNotifications: Notification[] = JSON.parse(localStorage.getItem('notifications')) || [];

  // create a writable store to hold the notifications
  export const notifications: Writable<Notification[]> = writable(initialNotifications);

  // create a function to add a notification
  export function add(
    message = 'Success',
    category = 'Success',
    duration = 1000,
  ): void {
    // generate a unique id for the notification
    const id: string = Math.random().toString(36).substr(2, 9);

    // push the notification to the store
    notifications.update((n: Notification[]) => [
      ...n,
      { id, message, category, duration },
    ]);

    // remove the notification after the duration
    setTimeout(() => {
      remove(id);
    }, duration * 1000);
  }

  export function remove(id: string): void {
    notifications.update((n: Notification[]) =>
      n.filter((x: Notification) => x.id !== id)
    );
  }

  export function removeAll(): void {
    notifications.set([]);
  }

  // Persist notifications in localStorage whenever the store changes
  notifications.subscribe((value: Notification[]) => {
    localStorage.setItem('notifications', JSON.stringify(value));
  });
</script>
