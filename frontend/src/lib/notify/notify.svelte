<script lang="ts">
import { type Notification, notifications, addNotification, removeNotification, removeAllNotifications } from './store.ts';


  export function add(
    message = 'Success',
    category = 'Success',
    duration = 10,
  ): void {
    console.log(duration);
    const id: string = Math.random().toString(36).substr(2, 9);

    const notification: Notification = {
      id,
      message,
      category,
      duration,
    };

    addNotification(notification);
    setTimeout(() => {
      removeNotification(id);
    }, duration*1000);
  }
</script>

<div class="toast toast-center w-96">
  {#if $notifications.length > 1}
  <div class="mt-2">
    <button class="btn btn-ghost" on:click={removeAllNotifications}>Close All ✕</button>
  </div>
  {/if}
  {#each $notifications as { id, message, category }}
  <div class={`alert alert-${category}`}>
    {message}
    <span class="btn btn-ghost btn-sm float-right pd-0" on:click={() => removeNotification(id)}>
      ✕
    </span>
  </div>
  {/each}
  <!--force these classes to be loaded-->
  <div class="alert-success" hidden></div>
  <div class="alert-error" hidden></div>
  <div class="alert-warning" hidden></div>
</div>
