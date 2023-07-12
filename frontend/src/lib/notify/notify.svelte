<script lang="ts">
  import { notifications, removeAllNotifications, removeNotification} from './index.js';
</script>

<div class="toast toast-center w-96">
{#if $notifications.length > 1}
<div class="flex items-end">
  <button class="btn btn-ghost" on:click={removeAllNotifications}>Close All ✕</button>
</div>
{/if}
{#each $notifications as { id, message, category }}
<div class={`alert alert-${category}`} in:slide out:blur>
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
