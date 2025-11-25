<script lang="ts">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import {Button} from '$lib/components/ui/button';
  import {type IAvailableUpdate, UpdateResult} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate';
  import Loading from '$lib/components/Loading.svelte';
  import {openReleaseUrl} from './utils';

  type Props = {
    checkPromise?: Promise<IAvailableUpdate | null>;
    installPromise?: Promise<UpdateResult>;
    installUpdate: (update: IAvailableUpdate) => Promise<void>;
    installProgress?: number;
  }

  let {
    checkPromise,
    installPromise,
    installUpdate,
    installProgress
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
      {$t`Installing Update...`}
      <!-- Don't show 0%, because that doesn't mean anything and we're not sure if the % actually works -->
      {#if installProgress}{installProgress}%{/if}
    </Button>
  {:then updateResult}
    <div class="flex items-center gap-4 p-4 rounded-lg bg-muted">
      {#if updateResult === UpdateResult.Success}
        <Icon icon="i-mdi-check-circle" />
        <p>{$t`Update installed successfully! Please restart the application.`}</p>
      {:else if updateResult === UpdateResult.Started}
        <Icon icon="i-mdi-information" />
        <!-- Apparently there's some unreliability in the update process.
         Hopefully the progress above will work and help -->
        <p>{$t`Update started in the background. Restart the application after the update is complete.`}</p>
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
    </div>
  {/await}
{:else if checkPromise}
  {#await checkPromise then availableUpdate}
    {#if availableUpdate}
      {#if availableUpdate.supportsAutoUpdate}
        <Button onclick={() => installUpdate(availableUpdate)} class="w-full" icon="i-mdi-download">
          {$t`Install Update`}
        </Button>
      {:else}
        <Button onclick={() => openReleaseUrl(availableUpdate.release)} target="_blank" class="w-full" icon="i-mdi-download">
          {$t`Download Update`}
        </Button>
      {/if}
    {/if}
  {/await}
{/if}
