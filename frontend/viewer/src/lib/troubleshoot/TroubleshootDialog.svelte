<script lang="ts">
  import {Button, Dialog, TextField} from 'svelte-ux';
  import {useTroubleshootingService} from '$lib/services/service-provider';

  const service = useTroubleshootingService();
  export let open = false;
  function readonly(e: HTMLInputElement|HTMLTextAreaElement) {
    e.setAttribute('readonly', '');
    return [];
  }
</script>
<Dialog bind:open={open} style="height: auto">
  <div slot="title">Troubleshoot</div>
  <div class="flex flex-col gap-4 items-start p-4">
    {#await service?.getDataDirectory() then value}
      <TextField actions={readonly} label="Data Directory" {value} class="self-stretch"/>
    {/await}
    <Button on:click={() => service?.openLogFile()}>Open Log file</Button>
  </div>
  <div slot="actions">
    <Button on:click={() => open = false}>Close</Button>
  </div>
</Dialog>
