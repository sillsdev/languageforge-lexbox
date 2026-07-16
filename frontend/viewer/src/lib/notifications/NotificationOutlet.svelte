<script lang="ts">
  import {AppNotification} from './notifications';
  import {useEventBus} from '$lib/services/event-bus';
  import type {IAppUpdateEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IAppUpdateEvent';
  import type {IUserNotificationEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IUserNotificationEvent';
  import {FwEventType} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/FwEventType';
  import {UserNotificationType} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/UserNotificationType';
  import {UserNotificationDuration} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/UserNotificationDuration';
  import {UpdateResult} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate/UpdateResult';
  import {t} from 'svelte-i18n-lingui';
  import {Toaster} from '$lib/components/ui/sonner';
  import {openReleaseUrl} from '$lib/updates/utils';

  const notificationTypes = {
    [UserNotificationType.Plain]: 'plain',
    [UserNotificationType.Success]: 'success',
    [UserNotificationType.Error]: 'error',
    [UserNotificationType.Info]: 'info',
    [UserNotificationType.Warning]: 'warning',
  } as const;

  const notificationDurations = {
    [UserNotificationDuration.Min]: 'min',
    [UserNotificationDuration.Short]: 'short',
    [UserNotificationDuration.Long]: 'long',
    [UserNotificationDuration.Infinite]: 'infinite',
  } as const;

  const eventBus = useEventBus();

  eventBus.onEventType<IAppUpdateEvent>(FwEventType.AppUpdate, event => {
    if (event.result == UpdateResult.ManualUpdateRequired) {
      AppNotification.displayAction($t`A new version of FieldWorks Lite is available.`, {
        callback: () => {
          void openReleaseUrl(event.release);
        },
        label: $t`Download`
      });
    } else if (event.result == UpdateResult.Success) {
      AppNotification.display(
        $t`FieldWorks Lite has been updated successfully. Please restart the app to apply the changes.`,
        {type: 'info', timeout: 'long'}
      );
    }
  }, {includeLast: true});

  eventBus.onEventType<IUserNotificationEvent>(FwEventType.UserNotification, event => {
    const {message, description, notificationType, duration, clipboardText} = event;
    if (notificationType === UserNotificationType.Error && clipboardText) {
      AppNotification.error(message, description, clipboardText);
    } else {
      AppNotification.display(message, {
        type: notificationTypes[notificationType],
        timeout: notificationDurations[duration],
        description,
      });
    }
  });
</script>

<!--
Prevent dialogs from disabling clicking on toasts
and prevent those clicks from dismissing the dialog.
-->
<Toaster
  class="pointer-events-auto"
  onpointerdown={e => {e.stopPropagation();}}
/>
