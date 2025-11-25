<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {useFwLiteConfig, useUpdateService} from '$lib/services/service-provider';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import {watch} from 'runed';
  import DevContent from '$lib/layout/DevContent.svelte';
  import {type IAvailableUpdate, UpdateResult} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate';
  import {Icon} from '$lib/components/ui/icon';
  import UpdateDialogContent from './UpdateDialogContent.svelte';
  import {useEventBus} from '$lib/services/event-bus';
  import {FwEventType, type IAppUpdateProgressEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events';
  import {getDownloadPageUrl, releaseNotesUrl} from './utils';

  let {open = $bindable()}: { open: boolean } = $props();
  const config = useFwLiteConfig();
  const updateService = useUpdateService();

  let checkPromise = $state<Promise<IAvailableUpdate | null>>();
  let installPromise = $state<Promise<UpdateResult>>();

  const eventBus = useEventBus();
  let installProgress = $state<number>();
  eventBus.onEventType<IAppUpdateProgressEvent>(FwEventType.AppUpdateProgress, event => {
    installProgress = event.percentage;
  });

  watch(() => open, () => {
    if (open) {
      // this is cached on the backend for a short time
      checkPromise = updateService.checkForUpdates();
    }
  });

  async function installUpdate(update: IAvailableUpdate) {
    installPromise = updateService.applyUpdate(update);
    try {
      const updateResult = await installPromise;
      if (updateResult === UpdateResult.Success) {
        checkPromise = undefined;
      }
    } catch (error) {
      console.error('Error installing update:', error);
      throw error;
    }
  }

  const appVersion = config.appVersion;
</script>

{#if open}
  <ResponsiveDialog open title={$t`Updates`}>
    <div class="flex flex-col gap-4">
      <div>
        <div>
          {$t`Application version`}: <span class="font-semibold">{appVersion}</span>
        </div>
        <DevContent>
          <div>
            {$t`Platform`}:
            <span class="font-semibold">{config.os}</span>
          </div>
        </DevContent>
      </div>

      <UpdateDialogContent
        {checkPromise}
        {installPromise}
        {installUpdate}
        {installProgress} />

      <div class="flex justify-center gap-3">
        <a
          class="inline-flex items-center flex-nowrap gap-1 text-sm font-medium underline underline-offset-4 hover:text-primary"
          href={getDownloadPageUrl()}
          target="_blank"
          rel="noopener noreferrer"
        >
          {$t`Download page`}
          <Icon icon="i-mdi-open-in-new" class="size-4" />
        </a>
        <a
          class="inline-flex items-center flex-nowrap gap-1 text-sm font-medium underline underline-offset-4 hover:text-primary"
          href={releaseNotesUrl}
          target="_blank"
          rel="noopener noreferrer"
        >
          {$t`Release notes`}
          <Icon icon="i-mdi-open-in-new" class="size-4" />
        </a>
      </div>
    </div>
  </ResponsiveDialog>
{/if}
