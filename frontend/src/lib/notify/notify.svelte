<script lang="ts">
  import { notifications, removeAllNotifications, removeNotification} from '.';
  import t from '$lib/i18n';
</script>

<div class="toast toast-center w-96">
{#if $notifications.length > 1}
<div class="flex items-end">
  <button class="btn btn-ghost float-right" on:click={removeAllNotifications}>{$t("notify.close_all")} ✕</button>
</div>
{/if}
{#each $notifications as note}
<div class={`alert alert-${note.category} in:slide out:blur`}>
  {note.message}
  <span class="btn btn-ghost btn-sm float-right pd-0" on:click={() => removeNotification(note)}>
    ✕
  </span>
</div>
{/each}
<!--force these classes to be loaded-->
<div class="alert-success" hidden></div>
<div class="alert-error" hidden></div>
<div class="alert-warning" hidden></div>
</div>
