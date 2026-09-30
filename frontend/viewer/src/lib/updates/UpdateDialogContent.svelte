<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {Button, XButton} from '$lib/components/ui/button';
  import {type IAvailableUpdate, UpdateResult} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate';
  import Loading from '$lib/components/Loading.svelte';
  import {formatFileSize} from '$lib/components/ui/format';
  import {getReleaseUrl} from './utils';

  type Props = {
    checkPromise?: Promise<IAvailableUpdate | undefined>;
    installPromise?: Promise<UpdateResult>;
    installUpdate: (update: IAvailableUpdate) => Promise<void>;
    restartApp: () => void;
    downloadProgress?: {bytesDownloaded: number; bytesPerSecond: number};
  }

  let {
    checkPromise,
    installPromise,
    installUpdate,
    restartApp,
    downloadProgress
  }: Props = $props();
</script>

{#if checkPromise}
  <div class="flex items-center gap-4 p-4 rounded-lg bg-muted">
    {#await checkPromise}
      <Loading />
      <p>{$t`Checking for updates...`}</p>
    {:then availableUpdate}
      {#if availableUpdate}
        <Icon icon="i-mdi-information" />
        <p>{$t`Update available: ${availableUpdate.release.version}`}</p>
      {:else}
        <Icon icon="i-mdi-check" />
        <p>{$t`You are running the latest version.`}</p>
      {/if}
    {:catch error}
      <Icon icon="i-mdi-alert-circle" />
      <p>
        {$t`Error checking for updates: ${error instanceof Error ? error.message : String(error)}`}
      </p>
    {/await}
  </div>
{/if}

{#if installPromise}
  {#await installPromise}
    <Button loading class="w-full" icon="i-mdi-download">
      {#if downloadProgress}
        {$t`Downloading update...`}
        {formatFileSize(downloadProgress.bytesDownloaded)} ({formatFileSize(downloadProgress.bytesPerSecond)}/s)
      {:else}
        {$t`Installing Update...`}
      {/if}
    </Button>
  {:then updateResult}
    <div class="flex items-center gap-4 p-4 rounded-lg bg-muted">
      {#if updateResult === UpdateResult.Success}
        <Icon icon="i-mdi-check-circle" />
        <p>{$t`Update downloaded. Restart to apply.`}</p>
      {:else if updateResult === UpdateResult.Started}
        <Icon icon="i-mdi-information" />
        <p>{$t`Update downloading in the background. Restart to apply once it's finished.`}</p>
      {:else if updateResult === UpdateResult.Failed}
        <Icon icon="i-mdi-alert-circle" />
        <p>{$t`Update failed to install.`}</p>
      {:else if updateResult === UpdateResult.ManualUpdateRequired}
        <!-- this should never happen, because we only provide a Download button if auto updating isn't supported -->
        <Icon icon="i-mdi-information" />
        <p>{$t`Manual update is required. Please follow the instructions provided.`}</p>
      {:else if updateResult === UpdateResult.Disallowed}
        <Icon icon="i-mdi-block-helper" />
        <p>{$t`Update was disallowed by permission settings.`}</p>
      {:else}
        <Icon icon="i-mdi-alert-circle" />
        <p>{$t`Unknown update result`}: {updateResult}</p>
      {/if}
      {#if updateResult !== UpdateResult.Success && updateResult !== UpdateResult.Started}
        <!-- let users retrigger the update if it didn't work -->
        <XButton onclick={() => installPromise = undefined} class="ml-auto border"/>
      {/if}
    </div>
    {#if updateResult === UpdateResult.Success || updateResult === UpdateResult.Started}
      <!-- the update only takes effect once the app restarts, so offer to do it now -->
      <Button onclick={restartApp} class="w-full" icon="i-mdi-restart">
        {$t`Restart`}
      </Button>
    {/if}
  {/await}
{:else if checkPromise}
  {#await checkPromise then availableUpdate}
    {#if availableUpdate}
      {#if availableUpdate.supportsAutoUpdate}
        <div class="flex flex-col items-center gap-1">
          <Button onclick={() => installUpdate(availableUpdate)} class="w-full" icon="i-mdi-download">
            {$t`Install Update`}
          </Button>
        </div>
      {:else}
        <Button href={getReleaseUrl(availableUpdate.release)} target="_blank"
          class="w-full"
          icon="i-mdi-download"
          rel="noopener noreferrer">
          {$t`Download Update`}
        </Button>
      {/if}
    {/if}
  {/await}
{/if}
