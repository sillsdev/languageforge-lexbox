<script lang="ts">
  import {Button} from '$lib/components/ui/button';
  import {useFwLiteConfig, useTroubleshootingService} from '$lib/services/service-provider';
  import {AppNotification} from '$lib/notifications/notifications';
  import {t} from 'svelte-i18n-lingui';
  import {QueryParamStateBool} from '$lib/utils/url.svelte';
  import Label from '$lib/components/ui/label/label.svelte';
  import {Input} from '$lib/components/ui/input';
  import {Switch} from '$lib/components/ui/switch';
  import {Textarea} from '$lib/components/ui/textarea';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import {featureFlags} from '$lib/feature-flags/feature-flags.svelte';
  import {CopyButton} from '$lib/components/ui/button';
  import ResponsiveDialog from '$lib/components/responsive-dialog/responsive-dialog.svelte';
  import * as AlertDialog from '$lib/components/ui/alert-dialog';
  import Loading from '$lib/components/Loading.svelte';
  import {resource} from 'runed';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import {FwLitePlatform} from '$lib/dotnet-types/generated-types/FwLiteShared/FwLitePlatform';

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
  let canShare = resource(() => service, async (s) => await s?.getCanShare());
  // web is configured through appsettings.json, only MAUI has a settings file to toggle or edit
  let canManageSettingsFile = resource(() => service, async (s) => await s?.getCanManageSettingsFile());
  const settingsService = $derived(canManageSettingsFile.current ? service : undefined);
  let verboseLogging = resource(() => settingsService, async (s) => await s?.getVerboseLogging());
  const regenerating = $derived(regeneratingOperation !== null);
  // Mobile platforms keep app data in private storage that no file manager can open.
  const canOpenFolders = $derived(config.os !== FwLitePlatform.Android && config.os !== FwLitePlatform.iOS);
  // in the phone drawer, one full-width button per row instead of a ragged wrap
  const actionRow = $derived(IsMobile.value ? 'flex flex-col gap-2' : 'flex flex-wrap gap-2');
  const actionButton = $derived(IsMobile.value ? 'justify-start' : '');
  let dataDirectory = resource(() => service, async (s) => await s?.getDataDirectory());
  let appDirectory = resource(() => service, async (s) => await s?.getAppDirectory());

  async function openFolder(tryOpen: () => Promise<boolean>) {
    if (!await tryOpen()) {
      AppNotification.display($t`Couldn't open the folder. Copy its path from this dialog instead.`, 'error');
    }
  }

  async function setVerboseLogging(enabled: boolean) {
    if (!service) return;
    try {
      await service.setVerboseLogging(enabled);
    } catch (e) {
      // a hand-edited settings file with broken JSON is an expected, actionable failure
      AppNotification.display(e instanceof Error ? e.message : $t`Failed to change detailed logging`, 'error');
    }
    // also puts the switch back in sync when the change failed
    await verboseLogging.refetch();
    await settingsFile.refetch();
  }

  let advancedOpen = $state(false);
  let settingsFile = resource(() => settingsService, async (s) => await s?.getSettingsFile());
  let settingsDraft = $state('');
  let settingsError = $state<string>();
  $effect(() => {
    if (settingsFile.current !== undefined) settingsDraft = settingsFile.current;
  });
  const settingsDirty = $derived(settingsDraft !== (settingsFile.current ?? ''));

  function discardSettings() {
    settingsDraft = settingsFile.current ?? '';
    settingsError = undefined;
  }

  async function saveSettingsFile() {
    if (!service) return;
    try {
      await service.setSettingsFile(settingsDraft);
    } catch (e) {
      // WriteSettingsFile's validation message: expected and fixable, so keep it next to the text
      const invalid = e instanceof Error ? /Settings are not valid.*?(?= \(Parameter '\w+'\)|$)/m.exec(e.message)?.[0] : undefined;
      if (!invalid) throw e;
      settingsError = invalid;
      return;
    }
    await settingsFile.refetch();
    await verboseLogging.refetch();
    AppNotification.display($t`Settings saved and applied`, {type: 'success', timeout: 'short'});
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
      AppNotification.display($t`Snapshots rebuilt`, 'success');
    } catch (e) {
      const message = e instanceof Error ? e.message : $t`Couldn't rebuild snapshots`;
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
      AppNotification.display($t`Search index rebuilt`, 'success');
    } catch (e) {
      const message = e instanceof Error ? e.message : $t`Couldn't rebuild search index`;
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
    class: 'sm:max-w-[min(calc(100%-32px),50rem)]',
    hideClose: regenerating,
    interactOutsideBehavior: regenerating ? 'ignore' : 'close',
    escapeKeydownBehavior: regenerating ? 'ignore' : 'close'
  }}>
  {#if regenerating}
    <div class="flex flex-col items-center justify-center gap-3 py-8 text-center">
      <Loading class="size-10" />
      {#if regeneratingOperation === 'snapshots'}
        <p>{$t`Rebuilding snapshots. This may take a long time. Do not close the app.`}</p>
      {:else}
        <p>{$t`Rebuilding search index. This may take a while. Do not close the app.`}</p>
      {/if}
    </div>
  {:else}
    <div class="flex flex-col gap-6">
      <p class="flex items-center gap-1 text-sm text-muted-foreground">
        <span>FieldWorks Lite <span class="font-medium text-foreground">{config.appVersion}</span> · {config.os}</span>
        <CopyButton
          variant="ghost"
          size="icon-xs"
          iconProps={{class: 'size-4'}}
          title={$t`Copy version`}
          text={`FieldWorks Lite ${config.appVersion} on ${config.os}`}
        />
      </p>

      {#if service}
        <section class="flex flex-col gap-3">
          <h3 class="font-medium">{$t`Support files`}</h3>
          <div class={actionRow}>
            {#if canShare.current}
              <Button variant="outline" icon="i-mdi-file-export" class={actionButton} onclick={() => service.shareLogFile()}>
                {$t`Share log file`}
              </Button>
            {/if}
            <Button variant="outline" icon="i-mdi-file-eye" class={actionButton} onclick={() => service.openLogFile()}>
              {$t`Open log file`}
            </Button>
            {#if canShare.current && projectCode}
              <Button variant="outline" icon="i-mdi-file-export" class={actionButton} onclick={() => shareProject()}>
                {$t`Share project data file`}
              </Button>
            {/if}
          </div>
          {#if settingsService && verboseLogging.current !== undefined}
            <div class="flex flex-col gap-1.5">
              <Switch label={$t`Detailed logging`}
                      checked={verboseLogging.current}
                      onCheckedChange={(enabled) => void setVerboseLogging(enabled)} />
              <p class="text-sm text-muted-foreground">
                {$t`Adds more detail to the log file, including sign-in details. Turn it off when support has what they need.`}
              </p>
            </div>
          {/if}
        </section>
      {/if}

      {#if service && (canOpenFolders || featureFlags.isDev)}
        <section class="flex flex-col gap-3">
          <h3 class="font-medium">{$t`Folders`}</h3>
          {#if !appDirectory.current}
            {@render folder('folder-projects', $t`Projects`, dataDirectory.current, () => service.tryOpenDataDirectory())}
          {:else if dataDirectory.current === appDirectory.current}
            {@render folder('folder-all', $t`Projects, logs and settings`, dataDirectory.current, () => service.tryOpenDataDirectory())}
          {:else}
            {@render folder('folder-projects', $t`Projects`, dataDirectory.current, () => service.tryOpenDataDirectory())}
            {@render folder('folder-app', $t`Logs and settings`, appDirectory.current, () => service.tryOpenAppDirectory())}
          {/if}
        </section>
      {/if}

      <Collapsible.Root bind:open={advancedOpen} class="border-t pt-4">
        <Collapsible.Trigger
          class="flex w-full cursor-pointer items-center gap-1 py-2 font-medium [&[data-state=open]>.i-mdi-chevron-down]:rotate-180">
          <i class="i-mdi-chevron-down size-5 transition-transform motion-reduce:transition-none"></i>
          {$t`Advanced`}
        </Collapsible.Trigger>
        <Collapsible.Content class="flex flex-col gap-6 pt-3">
          <p class="text-sm text-muted-foreground">{$t`Only change these when support asks you to.`}</p>
          {#if projectCode}
            <div class="flex flex-col gap-1.5">
              <div class="text-sm font-medium">{$t`Repair current project`}</div>
              <div class={actionRow}>
                <Button variant="outline" icon="i-mdi-magnify-scan" class={actionButton} onclick={() => regenerateSearchConfirmOpen = true}>
                  {$t`Rebuild search index`}
                </Button>
                <Button variant="outline" icon="i-mdi-database-refresh" class={actionButton} onclick={() => regenerateConfirmOpen = true}>
                  {$t`Rebuild snapshots`}
                </Button>
              </div>
            </div>
          {/if}
          <div class="flex flex-col gap-1.5">
            <Label for="release-channel">{$t`Release channel or feature flags`}</Label>
            <p id="release-channel-help" class="text-sm text-muted-foreground">
              {$t`Leave this empty unless support gives you a value.`}
            </p>
            <Input id="release-channel" aria-describedby="release-channel-help" bind:value={featureFlags.channel} />
          </div>
          {#if settingsService}
            <div class="flex flex-col gap-1.5">
              <Label for="settings-file">{$t`Settings file`}</Label>
              <p id="settings-file-help" class="text-sm text-muted-foreground">
                {$t`Paste the settings support sent you. Saving replaces the whole file and applies it right away. Save it empty to go back to the defaults. Changes to log file size or count need a restart.`}
              </p>
              <Textarea id="settings-file" bind:value={settingsDraft} oninput={() => settingsError = undefined}
                        aria-describedby="settings-file-help" aria-invalid={settingsError ? true : undefined}
                        rows={8} spellcheck={false} class="field-sizing-fixed font-mono text-xs" />
              {#if settingsError}
                <p role="alert" class="text-sm text-destructive">{settingsError}</p>
              {/if}
              <div class="flex gap-2">
                <Button size="sm" icon="i-mdi-content-save" disabled={!settingsDirty} onclick={() => saveSettingsFile()}>
                  {$t`Save and apply`}
                </Button>
                <Button variant="ghost" size="sm" disabled={!settingsDirty} onclick={discardSettings}>
                  {$t`Discard`}
                </Button>
              </div>
            </div>
          {/if}
        </Collapsible.Content>
      </Collapsible.Root>
    </div>
  {/if}
</ResponsiveDialog>

{#snippet folder(id: string, label: string, path: string | undefined, tryOpen: () => Promise<boolean>)}
  <div class="flex items-start justify-between gap-3">
    <div class="min-w-0">
      <div {id} class="text-sm font-medium">{label}</div>
      <!-- wrap only after a separator, so folder names stay whole -->
      <div class="text-sm text-muted-foreground break-words">
        {#each path?.split(/(?<=[\\/])/) ?? [] as part, i (i)}<wbr />{part}{/each}
      </div>
    </div>
    <div class="flex shrink-0 gap-1">
      <CopyButton variant="ghost" size="icon-sm" iconProps={{class: 'size-4'}} title={$t`Copy path`} aria-describedby={id} text={path ?? ''} />
      {#if canOpenFolders}
        <Button variant="outline" size="sm" icon="i-mdi-folder-open-outline" aria-describedby={id} onclick={() => openFolder(tryOpen)}>
          {$t`Open`}
        </Button>
      {/if}
    </div>
  </div>
{/snippet}

{#if regenerateConfirmOpen}
  <AlertDialog.Root bind:open={regenerateConfirmOpen}>
    <AlertDialog.Content>
      <AlertDialog.Header>
        <AlertDialog.Title>{$t`Rebuild snapshots?`}</AlertDialog.Title>
      </AlertDialog.Header>
      <AlertDialog.Description>
        <div class="space-y-2">
          <p>
            {$t`This rebuilds the project's stored data from its history of changes.`}
          </p>
          <p>
            {$t`On large projects this may take a long time. Do not close the app until it finishes.`}
          </p>
        </div>
      </AlertDialog.Description>
      <AlertDialog.Footer>
        <AlertDialog.Cancel>{$t`Cancel`}</AlertDialog.Cancel>
        <AlertDialog.Action onclick={() => confirmRegenerate()}>{$t`Rebuild`}</AlertDialog.Action>
      </AlertDialog.Footer>
    </AlertDialog.Content>
  </AlertDialog.Root>
{/if}

{#if regenerateSearchConfirmOpen}
  <AlertDialog.Root bind:open={regenerateSearchConfirmOpen}>
    <AlertDialog.Content>
      <AlertDialog.Header>
        <AlertDialog.Title>{$t`Rebuild search index?`}</AlertDialog.Title>
      </AlertDialog.Header>
      <AlertDialog.Description>
        <div class="space-y-2">
          <p>
            {$t`This rebuilds the search index from the dictionary entries.`}
          </p>
          <p>
            {$t`On large projects this may take a while. Do not close the app until it finishes.`}
          </p>
        </div>
      </AlertDialog.Description>
      <AlertDialog.Footer>
        <AlertDialog.Cancel>{$t`Cancel`}</AlertDialog.Cancel>
        <AlertDialog.Action onclick={() => confirmRegenerateSearch()}>{$t`Rebuild`}</AlertDialog.Action>
      </AlertDialog.Footer>
    </AlertDialog.Content>
  </AlertDialog.Root>
{/if}
