<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {useFwLiteConfig, useTroubleshootingService} from '$lib/services/service-provider';
  import {AppNotification} from '$lib/notifications/notifications';
  import {t} from 'svelte-i18n-lingui';
  import {QueryParamStateBool} from '$lib/utils/url.svelte';
  import InputShell from '$lib/components/ui/input/input-shell.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import {CopyButton} from '$lib/components/ui/button';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';

  const openQueryParam = new QueryParamStateBool({
    key: 'troubleshootDialogOpen',
    replaceOnDefaultValue: true,
    allowBack: true
  }, false);

  export function open(withProjectCode?: string): void {
    openQueryParam.current = true;
    projectCode = withProjectCode;
  }

  const service = useTroubleshootingService();
  const config = useFwLiteConfig();
  let projectCode = $state<string>();

  async function tryOpenDataDirectory() {
    if (!await service?.tryOpenDataDirectory()) {
      AppNotification.display($t`Failed to open data directory, use the path in the text field instead`, 'error');
    }
  }

  async function shareProject() {
    if (projectCode) {
      await service?.shareCrdtProject(projectCode);
    }
  }
</script>

<ResponsiveDialog bind:open={openQueryParam.current} title={$t`Troubleshoot`} disableBackHandler>
  <div class="flex flex-col gap-4 items-start">
    <div>
      <p class="flex items-baseline gap-1">
        {$t`Application version`}:
        <span class="font-semibold border-b">{config.appVersion}</span>
        <CopyButton
          variant="ghost"
          size="xs-icon"
          iconProps={{class: 'size-4'}}
          title={$t`Copy version`}
          text={`${config.appVersion} on ${config.os}`}
        />
      </p>
      <p class="flex items-baseline gap-1">
        {$t`Platform`}:
        <span class="font-semibold">{config.os}</span>
      </p>
    </div>
    {#if service}
      <div class="w-full">
        <Label>{$t`Data Directory`}</Label>
        <InputShell class="ps-2 pe-1">
          {#await service?.getDataDirectory() then value}
            {value}
          {/await}
          <Button variant="ghost" icon="i-mdi-folder-search" size="xs-icon" title={$t`Open Data Directory`}
                  onclick={() => tryOpenDataDirectory()}/>
        </InputShell>
      </div>
    {/if}
    {#if projectCode}
      <div class="flex gap-2">
        <Button variant="outline" onclick={() => shareProject()}>
          <i class="i-mdi-file-export"></i>
          {$t`Share project data file`}
        </Button>
      </div>
    {/if}
    {#if service}
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
    {/if}
  </div>
</ResponsiveDialog>
