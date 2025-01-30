<script lang="ts">
  import {AppNotification} from './notifications';
  import {Notification, Icon, Button, Collapse} from 'svelte-ux';
  import {
    mdiAlert,
    mdiAlertCircleOutline,
    mdiCheckCircleOutline,
    mdiClose,
    mdiInformationOutline
  } from '@mdi/js';

  const notifications = AppNotification.notifications;
</script>
{#if $notifications.length}
<div class="fixed bottom-0 left-1/2 -translate-x-1/2 z-50 flex flex-col items-center gap-2 p-4 min-w-[min(400px,100%)] overflow-y-auto overflow-x-hidden">
  {#each $notifications as notification (notification)}
    <div class="max-w-[100vw] min-w-[min(400px,100%)]">
      <Notification open
                    on:close={() => AppNotification.remove(notification)}
                    closeIcon
                    actionsPlacement="inline"
                    classes={{title: 'max-h-[30vh] overflow-y-auto px-2 whitespace-break-spaces', actions: notification.action ? '' : 'hidden'}}>
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
        <svelte:fragment slot="description">
          {#if notification.detail}
            <Collapse name="Details" class="px-2">
              <div class="whitespace-pre-wrap max-w-[60vw] max-h-64 p-2 overflow-auto border mt-2">
                {notification.detail}
              </div>
            </Collapse>
          {/if}
        </svelte:fragment>

        <svelte:fragment slot="actions">
          {#if notification.action}
            <Button color="primary" on:click={notification.action.callback}>{notification.action.label}</Button>
          {/if}
        </svelte:fragment>
      </Notification>
    </div>
  {/each}
  {#if $notifications.length > 1}
    <Button class="w-[min(400px,100%)] shadow-lg" variant="fill-outline" on:click={() => AppNotification.clear()} icon={mdiClose}>
      Close all
    </Button>
  {/if}
</div>
{/if}
