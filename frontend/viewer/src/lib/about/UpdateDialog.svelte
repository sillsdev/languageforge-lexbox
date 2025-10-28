<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {useFwLiteConfig, useUpdateService, useService} from '$lib/services/service-provider';
  import {Button} from '$lib/components/ui/button';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import {UpdateResult} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate/UpdateResult';
  import type {IAppUpdateEvent} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/IAppUpdateEvent';
  import {FwEventType} from '$lib/dotnet-types/generated-types/FwLiteShared/Events/FwEventType';
  import {DotnetService} from '$lib/dotnet-types';

  let {open = $bindable()}: { open: boolean } = $props();
  const config = useFwLiteConfig();
  const updateService = useUpdateService();
  const jsEventListener = useService(DotnetService.JsEventListener);

  let checking = $state(false);
  let updateResult = $state<UpdateResult | null>(null);
  let errorMessage = $state<string | null>(null);
  let eventSubscription: Promise<void> | null = null;

  async function subscribeToUpdateEvents() {
    try {
      while (checking) {
        const event = await jsEventListener.nextEventAsync();
        if (event && event.type === FwEventType.AppUpdate) {
          const updateEvent = event as IAppUpdateEvent;
          updateResult = updateEvent.result;
          break;
        }
      }
    } catch (error) {
      console.error('Error subscribing to update events:', error);
    }
  }

  async function checkForUpdates() {
    checking = true;
    updateResult = null;
    errorMessage = null;
    
    try {
      // Start listening for update events before triggering the check
      eventSubscription = subscribeToUpdateEvents();
      
      // Trigger the update check
      await updateService.checkForUpdates();
      
      // Wait for the event (with a timeout)
      const timeoutPromise = new Promise<void>((resolve) => {
        setTimeout(() => {
          resolve();
        }, 30000); // 30 second timeout
      });
      
      await Promise.race([eventSubscription, timeoutPromise]);
      
      // If we still don't have a result after timeout, check lastEvent
      if (updateResult === null) {
        const event = await jsEventListener.lastEvent(FwEventType.AppUpdate) as IAppUpdateEvent | null;
        if (event) {
          updateResult = event.result;
        } else {
          errorMessage = $t`Update check timed out.`;
          updateResult = UpdateResult.Failed;
        }
      }
    } catch (error) {
      console.error('Error checking for updates:', error);
      errorMessage = error instanceof Error ? error.message : String(error);
      updateResult = UpdateResult.Failed;
    } finally {
      checking = false;
      eventSubscription = null;
    }
  }

  function getUpdateMessage(): string {
    if (checking) {
      return $t`Checking for updates...`;
    }
    
    if (errorMessage) {
      return $t`Error checking for updates: ${errorMessage}`;
    }
    
    switch (updateResult) {
      case UpdateResult.Success:
        return $t`Update installed successfully! Please restart the application.`;
      case UpdateResult.Started:
        return $t`Update download started. The application will update when you restart it.`;
      case UpdateResult.ManualUpdateRequired:
        return $t`An update is available! Please visit the download page to update manually.`;
      case UpdateResult.Failed:
        return $t`Failed to check for updates. Please try again later.`;
      case UpdateResult.Unknown:
        return $t`No updates available. You are running the latest version.`;
      default:
        return $t`Check completed. Status unknown.`;
    }
  }

  function getUpdateIcon(): string {
    if (checking) {
      return 'i-mdi-loading';
    }
    
    switch (updateResult) {
      case UpdateResult.Success:
      case UpdateResult.Started:
        return 'i-mdi-check-circle';
      case UpdateResult.ManualUpdateRequired:
        return 'i-mdi-information';
      case UpdateResult.Failed:
        return 'i-mdi-alert-circle';
      case UpdateResult.Unknown:
      default:
        return 'i-mdi-check';
    }
  }

  const appVersion = config.appVersion;
  const downloadUrl = 'https://github.com/sillsdev/languageforge-lexbox/releases';
</script>

<ResponsiveDialog bind:open title={$t`Check for Updates`}>
  <div class="flex flex-col gap-4">
    <div class="flex flex-col gap-2">
      <div class="text-sm text-muted-foreground">
        {$t`Current version:`} <span class="font-mono font-semibold">{appVersion}</span>
      </div>
      <div class="text-sm text-muted-foreground">
        {$t`Platform:`} <span class="font-semibold">{config.os.toString()}</span>
      </div>
    </div>

    {#if updateResult !== null}
      <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
        <Icon 
          icon={getUpdateIcon()} 
          class={cn('size-6 flex-shrink-0', checking && 'animate-spin')}
        />
        <div class="flex-1 text-sm">
          {getUpdateMessage()}
        </div>
      </div>
    {/if}

    <div class="flex flex-col gap-2">
      <Button 
        onclick={() => void checkForUpdates()} 
        disabled={checking}
        class="w-full"
      >
        {#if checking}
          <Icon icon="i-mdi-loading" class="animate-spin mr-2" />
        {:else}
          <Icon icon="i-mdi-update" class="mr-2" />
        {/if}
        {$t`Check for Updates`}
      </Button>

      {#if updateResult === UpdateResult.ManualUpdateRequired}
        <Button 
          variant="outline"
          href={downloadUrl}
          target="_blank"
          class="w-full"
        >
          <Icon icon="i-mdi-download" class="mr-2" />
          {$t`Go to Download Page`}
        </Button>
      {/if}
    </div>
  </div>
</ResponsiveDialog>

<script context="module" lang="ts">
  import {cn} from '$lib/utils';
</script>
