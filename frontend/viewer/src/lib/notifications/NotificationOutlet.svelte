<script lang="ts">
  import {AppNotification} from './notifications';
  import {Notification, Icon, Button} from 'svelte-ux';
  import {
    mdiAlert,
    mdiAlertCircleOutline,
    mdiCheckCircleOutline,
    mdiInformationOutline
  } from '@mdi/js';

  const notifications = AppNotification.notifications;
</script>
{#if $notifications.length}
<div class="fixed bottom-0 z-50 flex flex-col gap-2 p-4 w-full overflow-y-auto">
  {#each $notifications as notification}
    <div class="w-[400px] mx-auto">
      <Notification open closeIcon actionsPlacement="inline">
        <div slot="icon">
          {#if notification.type === 'success'}
            <Icon path={mdiCheckCircleOutline} size="1.5rem" class="text-success"/>
          {:else if notification.type === 'error'}
            <Icon path={mdiAlert} size="1.5rem" class="text-danger"/>
          {:else if notification.type === 'info'}
            <Icon path={mdiInformationOutline} size="1.5rem" class="text-info"/>
          {:else if notification.type === 'warning'}
            <Icon path={mdiAlertCircleOutline} size="1.5rem" class="text-warning"/>
          {/if}
        </div>
        <div slot="title">{notification.message}</div>
        <div slot="actions">
          {#if notification.action}
            <Button color="primary" on:click={notification.action.callback}>{notification.action.label}</Button>
          {/if}
        </div>
      </Notification>
    </div>
  {/each}
</div>
{/if}
