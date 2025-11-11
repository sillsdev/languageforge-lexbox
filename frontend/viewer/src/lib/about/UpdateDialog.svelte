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

  let checkPromise = $state<Promise<IAvailableUpdate | null> | null>(null);
  let installPromise = $state<Promise<void> | null>(null);
  let installSuccess = $state(false);

  async function checkForUpdates() {
    installSuccess = false;
    checkPromise = updateService.checkForUpdates();
    return checkPromise;
  }

  async function installUpdate(update: IAvailableUpdate) {
    installPromise = updateService.applyUpdate(update);
    try {
      await installPromise;
      installSuccess = true;
    } catch (error) {
      console.error('Error installing update:', error);
      throw error;
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
        {$t`Platform:`} <span class="font-semibold">{config.os}</span>
      </div>
    </div>

    {#if checkPromise}
      {#await checkPromise}
        <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
          <Icon icon="i-mdi-loading" class="size-6 flex-shrink-0 animate-spin" />
          <div class="flex-1 text-sm">
            {$t`Checking for updates...`}
          </div>
        </div>
      {:then availableUpdate}
        {#if installSuccess}
          <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
            <Icon icon="i-mdi-check-circle" class="size-6 flex-shrink-0 text-success" />
            <div class="flex-1 text-sm">
              {$t`Update installed successfully! Please restart the application.`}
            </div>
          </div>
        {:else if availableUpdate}
          <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
            <Icon icon="i-mdi-information" class="size-6 flex-shrink-0 text-info" />
            <div class="flex-1 text-sm">
              {$t`Update available: ${availableUpdate.release.version}`}
            </div>
          </div>
        {:else}
          <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
            <Icon icon="i-mdi-check" class="size-6 flex-shrink-0 text-success" />
            <div class="flex-1 text-sm">
              {$t`You are running the latest version.`}
            </div>
          </div>
        {/if}
      {:catch error}
        <div class="flex items-start gap-3 p-4 rounded-lg bg-muted">
          <Icon icon="i-mdi-alert-circle" class="size-6 flex-shrink-0 text-destructive" />
          <div class="flex-1 text-sm">
            {$t`Error checking for updates: ${error instanceof Error ? error.message : String(error)}`}
          </div>
        </div>
      {/await}
    {/if}

    {#if checkPromise}
      {#await checkPromise then availableUpdate}
        {#if !availableUpdate || installSuccess}
          <Button onclick={() => void checkForUpdates()} class="w-full" icon="i-mdi-update">
            {$t`Check for Updates`}
          </Button>
        {:else if availableUpdate.supportsAutoUpdate}
          {#if installPromise}
            {#await installPromise}
              <Button disabled loading class="w-full" icon="i-mdi-download">
                {$t`Installing Update...`}
              </Button>
            {:then}
              <Button onclick={() => void checkForUpdates()} class="w-full" icon="i-mdi-update">
                {$t`Check for Updates`}
              </Button>
            {:catch error}
              <Button onclick={() => void installUpdate(availableUpdate)} class="w-full" icon="i-mdi-download">
                {$t`Install Update`}
              </Button>
              <div class="text-sm text-destructive">
                {$t`Error: ${error instanceof Error ? error.message : String(error)}`}
              </div>
            {/await}
          {:else}
            <Button onclick={() => void installUpdate(availableUpdate)} class="w-full" icon="i-mdi-download">
              {$t`Install Update`}
            </Button>
          {/if}
        {:else}
          <Button variant="default" href={availableUpdate.release.url} target="_blank" class="w-full" icon="i-mdi-download">
            {$t`Go to Download Page`}
          </Button>
        {/if}
      {:catch}
        <Button onclick={() => void checkForUpdates()} class="w-full" icon="i-mdi-update">
          {$t`Check for Updates`}
        </Button>
      {/await}
    {:else}
      <Button onclick={() => void checkForUpdates()} class="w-full" icon="i-mdi-update">
        {$t`Check for Updates`}
      </Button>
    {/if}
  </div>
</ResponsiveDialog>
