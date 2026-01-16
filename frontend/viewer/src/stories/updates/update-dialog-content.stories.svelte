<script module lang="ts">
  import {defineMeta} from '@storybook/addon-svelte-csf';
  import UpdateDialogContent from '$lib/updates/UpdateDialogContent.svelte';

  const {Story} = defineMeta({
    component: UpdateDialogContent,
    args: {
      installUpdate: async () => {
        await new Promise(resolve => setTimeout(resolve, 750));
      },
    },
  });
</script>
<script lang="ts">
  import type {IAvailableUpdate} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate';
  import {UpdateResult} from '$lib/dotnet-types/generated-types/FwLiteShared/AppUpdate';
  import {DemoStoryError} from '../demo-story-error';

  const autoUpdate: IAvailableUpdate = {
    release: {
      version: 'v1.2.3',
      url: 'https://sil.org/',
    },
    supportsAutoUpdate: true,
  };

  const manualUpdate: IAvailableUpdate = {
    release: {
      version: 'v2.0.0',
      url: 'https://sil.org/',
    },
    supportsAutoUpdate: false,
  };

  function pendingCheck(): Promise<IAvailableUpdate | null> {
    return new Promise(() => {});
  }

  function failedCheck(message: string): Promise<never> {
    return Promise.reject(new DemoStoryError(message));
  }

  function completedCheck(update: IAvailableUpdate | null): Promise<IAvailableUpdate | null> {
    return Promise.resolve(update);
  }

  function pendingInstall(): Promise<UpdateResult> {
    return new Promise(() => {});
  }

  function completedInstall(result: UpdateResult): Promise<UpdateResult> {
    return Promise.resolve(result);
  }
</script>

<Story name="Checking">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent {...args} checkPromise={pendingCheck()} />
    </div>
  {/snippet}
</Story>

<Story name="Update Available (Auto)">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent {...args} checkPromise={completedCheck(autoUpdate)} />
    </div>
  {/snippet}
</Story>

<Story name="Update Available (Manual Download)">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent {...args} checkPromise={completedCheck(manualUpdate)} />
    </div>
  {/snippet}
</Story>

<Story name="Up To Date">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent {...args} checkPromise={completedCheck(null)} />
    </div>
  {/snippet}
</Story>

<Story name="Check Error">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent {...args} checkPromise={failedCheck('Failed to reach update service')} />
    </div>
  {/snippet}
</Story>

<Story name="Installing">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent
        {...args}
        checkPromise={completedCheck(autoUpdate)}
        installPromise={pendingInstall()}
        installProgress={86}
      />
    </div>
  {/snippet}
</Story>

<Story name="Install Success">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent
        {...args}
        checkPromise={completedCheck(autoUpdate)}
        installPromise={completedInstall(UpdateResult.Success)}
      />
    </div>
  {/snippet}
</Story>

<Story name="Install Started">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent
        {...args}
        checkPromise={completedCheck(autoUpdate)}
        installPromise={completedInstall(UpdateResult.Started)}
      />
    </div>
  {/snippet}
</Story>

<Story name="Install Failed">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent
        {...args}
        checkPromise={completedCheck(autoUpdate)}
        installPromise={completedInstall(UpdateResult.Failed)}
      />
    </div>
  {/snippet}
</Story>

<Story name="Manual Update Required">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent
        {...args}
        checkPromise={completedCheck(manualUpdate)}
        installPromise={completedInstall(UpdateResult.ManualUpdateRequired)}
      />
    </div>
  {/snippet}
</Story>

<Story name="Install Disallowed">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent
        {...args}
        checkPromise={completedCheck(autoUpdate)}
        installPromise={completedInstall(UpdateResult.Disallowed)}
      />
    </div>
  {/snippet}
</Story>

<Story name="Unknown Result">
  {#snippet template(args)}
    <div class="max-w-md space-y-4">
      <UpdateDialogContent
        {...args}
        checkPromise={completedCheck(autoUpdate)}
        installPromise={completedInstall(UpdateResult.Unknown)}
      />
    </div>
  {/snippet}
</Story>
