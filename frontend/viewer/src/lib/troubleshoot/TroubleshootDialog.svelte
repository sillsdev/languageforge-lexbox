<script lang="ts">
  import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '$lib/components/ui/dialog';
  import { Button } from '$lib/components/ui/button';
  import { Field } from 'svelte-ux';
  import { useFwLiteConfig, useTroubleshootingService } from '$lib/services/service-provider';
  import { AppNotification } from '$lib/notifications/notifications';
  import { t } from 'svelte-i18n-lingui';

  const service = useTroubleshootingService();
  const config = useFwLiteConfig();
  export let open = false;

  async function tryOpenDataDirectory() {
    if (!await service?.tryOpenDataDirectory()) {
      AppNotification.display($t`Failed to open data directory, use the path in the text field instead`, 'error');
    }
  }
</script>

<Dialog bind:open>
  <DialogContent>
    <DialogHeader>
      <DialogTitle>{$t`Troubleshoot`}</DialogTitle>
    </DialogHeader>
    <div class="flex flex-col gap-4 items-start">
      <p>{$t`Application version`}: <span class="font-mono text-muted-foreground border-b">{config.appVersion}</span></p>

      {#await service?.getDataDirectory() then value}
        <Field label={$t`Data Directory`} {value} class="self-stretch">
          <span slot="append">
            <Button variant="ghost" size="icon" title={$t`Open Data Directory`} onclick={() => tryOpenDataDirectory()}>
              <i class="i-mdi-folder-search"></i>
            </Button>
          </span>
        </Field>
      {/await}
      <div class="flex gap-2">
        <Button variant="outline" onclick={() => service?.openLogFile()}>
          <i class="i-mdi-file-eye"></i>
          {$t`Open Log file`}
        </Button>
        <Button variant="outline" onclick={() => service?.shareLogFile()}>
          <i class="i-mdi-file-export"></i>
          {$t`Share Log file`}
        </Button>
      </div>
    </div>
  </DialogContent>
</Dialog>
