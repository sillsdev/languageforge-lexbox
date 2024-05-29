<script lang="ts">
  import t from '$lib/i18n';
  import { BadgeButton } from '$lib/components/Badges';
  import { slide, blur } from 'svelte/transition';
  import { useNotifications } from '.';

  const { notifications, removeAllNotifications, removeNotification } = useNotifications();
</script>

{#if $notifications.length > 0}
  <div class="toast toast-center prose z-20">
    {#if $notifications.length > 1}
      <div class="flex justify-end" in:slide out:blur>
        <BadgeButton on:click={removeAllNotifications}>{$t('notify.close_all')}<span class="ml-2">✕</span></BadgeButton>
      </div>
    {/if}
    {#each $notifications as note (note)}
      <div class="alert {note.category ?? ''}" in:slide out:blur>
        {note.message}
        <button on:click={() => removeNotification(note)} class="btn btn-circle btn-sm btn-ghost">✕</button>
      </div>
    {/each}
  </div>
{/if}
