<script lang="ts">
  import {Button, Dialog, Field} from 'svelte-ux';
  import {useTroubleshootingService} from '$lib/services/service-provider';
  import {AppNotification} from '$lib/notifications/notifications';
  import {mdiFileEye, mdiFolderSearch} from '@mdi/js';

  const service = useTroubleshootingService();
  export let open = false;

  async function tryOpenDataDirectory() {
    if (!await service?.tryOpenDataDirectory()) {
      AppNotification.display('Failed to open data directory, use the path in the text field instead', 'error');
    }
  }
</script>
<Dialog bind:open={open} style="height: auto">
  <div slot="title">Troubleshoot</div>
  <div class="flex flex-col gap-4 items-start p-4">
    {#await service?.getDataDirectory() then value}
      <Field label="Data Directory" {value} class="self-stretch">
          <span slot="append">
            <Button icon={mdiFolderSearch} title="Open Data Directory" class="text-surface-content/50 p-2" on:click={() => tryOpenDataDirectory()}/>
          </span>
      </Field>
    {/await}
    <Button variant="fill-light" icon={mdiFileEye} on:click={() => service?.openLogFile()}>Open Log file</Button>
  </div>
  <div slot="actions">
    <Button on:click={() => open = false}>Close</Button>
  </div>
</Dialog>
