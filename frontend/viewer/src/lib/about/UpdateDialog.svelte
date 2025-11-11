<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {useFwLiteConfig, useUpdateService} from '$lib/services/service-provider';
  import {Button} from '$lib/components/ui/button';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import type {IAvailableUpdate} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate/IAvailableUpdate';

  let {open = $bindable()}: { open: boolean } = $props();
  const config = useFwLiteConfig();
  const updateService = useUpdateService();

  let checking = $state(false);
  let installing = $state(false);
  let availableUpdate = $state<IAvailableUpdate | null>(null);
  let checkComplete = $state(false);
  let errorMessage = $state<string | null>(null);
  let installSuccess = $state(false);

  async function checkForUpdates() {
    checking = true;
    availableUpdate = null;
    checkComplete = false;
    errorMessage = null;
    installSuccess = false;
    
    try {
      const update = await updateService.checkForUpdates();
      availableUpdate = update;
      checkComplete = true;
    } catch (error) {
      console.error('Error checking for updates:', error);
      errorMessage = error instanceof Error ? error.message : String(error);
      checkComplete = true;
    } finally {
      checking = false;
    }
  }

  async function installUpdate() {
    if (!availableUpdate) return;
    
    installing = true;
    errorMessage = null;
    
    try {
      await updateService.applyUpdate(availableUpdate);
      installSuccess = true;
    } catch (error) {
      console.error('Error installing update:', error);
      errorMessage = error instanceof Error ? error.message : String(error);
    } finally {
      installing = false;
    }
  }

  const appVersion = config.appVersion;
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

    {#if checking}
      <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
        <Icon icon="i-mdi-loading" class="size-6 flex-shrink-0 animate-spin" />
        <div class="flex-1 text-sm">
          {$t`Checking for updates...`}
        </div>
      </div>
    {:else if errorMessage}
      <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
        <Icon icon="i-mdi-alert-circle" class="size-6 flex-shrink-0 text-destructive" />
        <div class="flex-1 text-sm">
          {$t`Error checking for updates: ${errorMessage}`}
        </div>
      </div>
    {:else if installSuccess}
      <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
        <Icon icon="i-mdi-check-circle" class="size-6 flex-shrink-0 text-success" />
        <div class="flex-1 text-sm">
          {$t`Update installed successfully! Please restart the application.`}
        </div>
      </div>
    {:else if checkComplete && availableUpdate}
      <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
        <Icon icon="i-mdi-information" class="size-6 flex-shrink-0 text-info" />
        <div class="flex-1 text-sm">
          {$t`Update available: ${availableUpdate.release.version}`}
        </div>
      </div>
    {:else if checkComplete && !availableUpdate}
      <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
        <Icon icon="i-mdi-check" class="size-6 flex-shrink-0 text-success" />
        <div class="flex-1 text-sm">
          {$t`You are running the latest version.`}
        </div>
      </div>
    {/if}

    <div class="flex flex-col gap-2">
      {#if !availableUpdate || installSuccess}
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
      {:else if availableUpdate.supportsAutoUpdate}
        <Button 
          onclick={() => void installUpdate()} 
          disabled={installing}
          class="w-full"
        >
          {#if installing}
            <Icon icon="i-mdi-loading" class="animate-spin mr-2" />
          {:else}
            <Icon icon="i-mdi-download" class="mr-2" />
          {/if}
          {$t`Install Update`}
        </Button>
        <Button 
          variant="outline"
          onclick={() => void checkForUpdates()} 
          disabled={checking || installing}
          class="w-full"
        >
          <Icon icon="i-mdi-update" class="mr-2" />
          {$t`Check Again`}
        </Button>
      {:else}
        <Button 
          variant="default"
          href={availableUpdate.release.url}
          target="_blank"
          class="w-full"
        >
          <Icon icon="i-mdi-download" class="mr-2" />
          {$t`Go to Download Page`}
        </Button>
        <Button 
          variant="outline"
          onclick={() => void checkForUpdates()} 
          disabled={checking}
          class="w-full"
        >
          <Icon icon="i-mdi-update" class="mr-2" />
          {$t`Check Again`}
        </Button>
      {/if}
    </div>
  </div>
</ResponsiveDialog>

<script context="module" lang="ts">
  import {cn} from '$lib/utils';
</script>
