<script lang="ts">
  import { Dialog, DialogContent, DialogHeader, DialogTitle } from '$lib/components/ui/dialog';
  import { Button } from '$lib/components/ui/button';
  import { useFwLiteConfig, useTroubleshootingService } from '$lib/services/service-provider';
  import { AppNotification } from '$lib/notifications/notifications';
  import { t } from 'svelte-i18n-lingui';
  import {QueryParamStateBool} from '$lib/utils/url.svelte';
  import InputShell from '$lib/components/ui/input/input-shell.svelte';
  import Label from '$lib/components/ui/label/label.svelte';

  const openQueryParam = new QueryParamStateBool({ key: 'troubleshootDialogOpen', replaceOnDefaultValue: true, allowBack: true }, false);

  export function open(): void {
    openQueryParam.current = true;
  }

  const service = useTroubleshootingService();
  const config = useFwLiteConfig();

  async function tryOpenDataDirectory() {
    if (!await service?.tryOpenDataDirectory()) {
      AppNotification.display($t`Failed to open data directory, use the path in the text field instead`, 'error');
    }
  }
</script>

<Dialog bind:open={openQueryParam.current}>
  <DialogContent class="sm:min-h-fit">
    <DialogHeader>
      <DialogTitle>{$t`Troubleshoot`}</DialogTitle>
    </DialogHeader>
    <div class="flex flex-col gap-4 items-start">
      <p>{$t`Application version`}: <span class="font-mono text-muted-foreground border-b">{config.appVersion}</span></p>
      <div class="w-full">
        <Label>{$t`Data Directory`}</Label>
        <InputShell class="ps-2 pe-1">
          {#await service?.getDataDirectory() then value}
            {value}
          {/await}
          <Button variant="ghost" icon="i-mdi-folder-search" size="xs-icon" title={$t`Open Data Directory`} onclick={() => tryOpenDataDirectory()} />
        </InputShell>
      </div>
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
