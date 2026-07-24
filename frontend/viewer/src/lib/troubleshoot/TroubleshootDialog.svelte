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
  import * as AlertDialog from '$lib/components/ui/alert-dialog';
  import Loading from '$lib/components/Loading.svelte';
  import {watch} from 'runed';
  import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';
  import {isDev} from '$lib/layout/DevContent.svelte';

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
  type RegeneratingOperation = 'snapshots' | 'search';
  let regeneratingOperation = $state<RegeneratingOperation | null>(null);
  let regenerateConfirmOpen = $state(false);
  let regenerateSearchConfirmOpen = $state(false);
  let canShare = $state(false);
  // Defer until the dialog opens — Troubleshoot is always mounted in the sidebar.
  watch(() => openQueryParam.current, () => {
    if (openQueryParam.current && service) {
      void service.getCanShare().then(value => { canShare = value ?? false; });
    }
  });
  const regenerating = $derived(regeneratingOperation !== null);
  // Mobile platforms keep app data in private storage that no file manager can open.
  const canOpenDataDirectory = $derived(config.os !== FwLitePlatform.Android && config.os !== FwLitePlatform.iOS);

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

  async function confirmRegenerate() {
    regenerateConfirmOpen = false;
    if (!projectCode || !service) return;
    regeneratingOperation = 'snapshots';
    try {
      await service.regenerateHarmonySnapshots(projectCode);
      AppNotification.display($t`Harmony snapshots regenerated successfully`, 'success');
    } catch (e) {
      const message = e instanceof Error ? e.message : $t`Failed to regenerate Harmony snapshots`;
      AppNotification.display(message, 'error');
    } finally {
      regeneratingOperation = null;
    }
  }

  async function confirmRegenerateSearch() {
    regenerateSearchConfirmOpen = false;
    if (!projectCode || !service) return;
    regeneratingOperation = 'search';
    try {
      await service.regenerateEntrySearchTable(projectCode);
      AppNotification.display($t`Search index regenerated successfully`, 'success');
    } catch (e) {
      const message = e instanceof Error ? e.message : $t`Failed to regenerate search index`;
      AppNotification.display(message, 'error');
    } finally {
      regeneratingOperation = null;
    }
  }
</script>

<ResponsiveDialog
  bind:open={openQueryParam.current}
  title={$t`Troubleshoot`}
  disableBackHandler
  contentProps={{
    hideClose: regenerating,
    interactOutsideBehavior: regenerating ? 'ignore' : 'close',
    escapeKeydownBehavior: regenerating ? 'ignore' : 'close'
  }}>
  <div class="flex flex-col gap-4 items-start">
    {#if regenerating}
      <div class="w-full flex flex-col items-center justify-center gap-3 py-8 text-center">
        <Loading class="size-10" />
        {#if regeneratingOperation === 'snapshots'}
          <p>{$t`Regenerating Harmony snapshots. This may take a long time — do not close the app.`}</p>
        {:else}
          <p>{$t`Regenerating search index. This may take a while — do not close the app.`}</p>
        {/if}
      </div>
    {:else}
      <div>
        <p class="flex items-baseline gap-1">
          {$t`FieldWorks Lite version`}:
          <span class="font-semibold border-b">
            {config.appVersion}
          </span>
          <CopyButton
            variant="ghost"
            size="icon-xs"
            iconProps={{class: 'size-4'}}
            title={$t`Copy version`}
            text={`FieldWorks Lite ${config.appVersion} on ${config.os}`}
          />
        </p>
        <p class="flex items-baseline gap-1">
          {$t`Platform`}:
          <span class="font-semibold">{config.os}</span>
        </p>
      </div>
      {#if service && (canOpenDataDirectory || $isDev)}
        <div class="w-full flex flex-col gap-1.5">
          <Label>{$t`Data Directory`}</Label>
          <InputShell class="ps-2 pe-1">
            {#await service?.getDataDirectory() then value}
              {value}
            {/await}
            {#if canOpenDataDirectory}
              <Button variant="ghost" icon="i-mdi-folder-search" size="icon-xs" title={$t`Open Data Directory`}
                      onclick={() => tryOpenDataDirectory()}/>
            {/if}
          </InputShell>
        </div>
      {/if}
      {#if projectCode}
        <div class="flex flex-wrap gap-2">
          {#if canShare}
            <Button variant="outline" onclick={() => shareProject()}>
              <i class="i-mdi-file-export"></i>
              {$t`Share project data file`}
            </Button>
          {/if}
          <Button variant="outline" onclick={() => regenerateConfirmOpen = true}>
            <i class="i-mdi-database-refresh"></i>
            {$t`Regenerate Harmony snapshots`}
          </Button>
          <Button variant="outline" onclick={() => regenerateSearchConfirmOpen = true}>
            <i class="i-mdi-magnify-scan"></i>
            {$t`Regenerate search index`}
          </Button>
        </div>
      {/if}
      {#if service}
        <div class="flex gap-2">
          <Button variant="outline" onclick={() => service?.openLogFile()}>
            <i class="i-mdi-file-eye"></i>
            {$t`Open Log file`}
          </Button>
          {#if canShare}
            <Button variant="outline" onclick={() => service?.shareLogFile()}>
              <i class="i-mdi-file-export"></i>
              {$t`Share Log file`}
            </Button>
          {/if}
        </div>
      {/if}
    {/if}
  </div>
</ResponsiveDialog>

{#if regenerateConfirmOpen}
  <AlertDialog.Root bind:open={regenerateConfirmOpen}>
    <AlertDialog.Content>
      <AlertDialog.Header>
        <AlertDialog.Title>{$t`Regenerate Harmony snapshots?`}</AlertDialog.Title>
      </AlertDialog.Header>
      <AlertDialog.Description>
        <div class="space-y-2">
          <p>
            {$t`This rebuilds snapshot and projected tables from commit history.`}
          </p>
          <p>
            {$t`On large projects this may take a long time. Do not close the app or interrupt the process.`}
          </p>
        </div>
      </AlertDialog.Description>
      <AlertDialog.Footer>
        <AlertDialog.Cancel>{$t`Cancel`}</AlertDialog.Cancel>
        <AlertDialog.Action onclick={() => confirmRegenerate()}>{$t`Regenerate`}</AlertDialog.Action>
      </AlertDialog.Footer>
    </AlertDialog.Content>
  </AlertDialog.Root>
{/if}

{#if regenerateSearchConfirmOpen}
  <AlertDialog.Root bind:open={regenerateSearchConfirmOpen}>
    <AlertDialog.Content>
      <AlertDialog.Header>
        <AlertDialog.Title>{$t`Regenerate search index?`}</AlertDialog.Title>
      </AlertDialog.Header>
      <AlertDialog.Description>
        <div class="space-y-2">
          <p>
            {$t`This rebuilds the full-text search index from dictionary entries.`}
          </p>
          <p>
            {$t`On large projects this may take a while. Do not close the app or interrupt the process.`}
          </p>
        </div>
      </AlertDialog.Description>
      <AlertDialog.Footer>
        <AlertDialog.Cancel>{$t`Cancel`}</AlertDialog.Cancel>
        <AlertDialog.Action onclick={() => confirmRegenerateSearch()}>{$t`Regenerate`}</AlertDialog.Action>
      </AlertDialog.Footer>
    </AlertDialog.Content>
  </AlertDialog.Root>
{/if}
