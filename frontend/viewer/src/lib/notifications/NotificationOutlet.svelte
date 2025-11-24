<script lang="ts">
  import {AppNotification} from './notifications';
  import {useEventBus} from '$lib/services/event-bus';
  import type {IAppUpdateEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IAppUpdateEvent';
  import {FwEventType} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/FwEventType';
  import {UpdateResult} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate/UpdateResult';
  import {t} from 'svelte-i18n-lingui';
  import {useFwLiteConfig} from '$lib/services/service-provider';
  import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
  import {Toaster} from '$lib/components/ui/sonner';
  import {openUrl} from '$lib/services/url-opener';

  const eventBus = useEventBus();

  const updateUrls: Partial<Record<FwLitePlatform, string>> = {
    [FwLitePlatform.Android]: 'https://play.google.com/store/apps/details?id=org.sil.FwLiteMaui',
  };

  eventBus.onEventType<IAppUpdateEvent>(FwEventType.AppUpdate, event => {
    if (event.result == UpdateResult.ManualUpdateRequired) {
      AppNotification.displayAction($t`A new version of FieldWorks Lite is available.`, {
        callback: () => {
          const fwliteConfig = useFwLiteConfig();
          const url = updateUrls[fwliteConfig.os] ?? event.release.url;
          void openUrl(url);
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
</script>

<!--
Prevent dialogs from disabling clicking on toasts
and prevent those clicks from dismissing the dialog.
-->
<Toaster
  class="pointer-events-auto"
  onpointerdown={e => {e.stopPropagation();}}
/>
