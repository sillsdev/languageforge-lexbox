<script lang="ts">
    import { writable, type Writable } from 'svelte/store';
    interface Notification {
      id: string;
      message: string;
      category: string;
      duration: number;
    }

    // create a store to hold the notifications
    export const notifications: Writable<Notification[]> = writable([]);

    // create a function to add a notification
    export function add(
      message: string,
      category: string,
      duration: number
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

    // create a function to remove a notification by id
    export function remove(id: string): void {
      notifications.update((n: Notification[]) =>
        n.filter((x: Notification) => x.id !== id)
      );
    }
    export function removeAll(): void {
    notifications.set([]);
  }
  </script>

<div class="toast toast-center w-96">
  {#if $notifications.length > 1}
  <div class="mt-2">
    <button class="btn btn-ghost" on:click={removeAll}>Close All ✕</button>
  </div>
  {/if}
  {#each $notifications as { id, message, category }}
  <div class={`alert alert-${category}`}>
    {message}
    <span class="btn btn-ghost btn-sm float-right pd-0" on:click={() => remove(id)}>
      ✕
    </span>
  </div>
  {/each}
  <div class="alert-success" hidden></div>
  <div class="alert-error" hidden></div>
  <div class="alert-warning" hidden></div>
</div>

