<script lang="ts">
  import type {Snippet} from 'svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {navigate, useRouter} from 'svelte-routing';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import EditEntryDialog from '$lib/entry-editor/EditEntryDialog.svelte';
  import {PluginApiAdapter} from './plugin-api-adapter';
  import {PluginHost} from './plugin-host';
  import {PLUGIN_IFRAME_ALLOW, PLUGIN_IFRAME_SANDBOX} from './plugin-srcdoc';
  import type {PluginManifest} from './plugin-manifest';
  import {createMemoryStorageBackend, PluginStorage} from './plugin-local-data';
  import type {OpenEntryMode, PluginWriteMode, PluginWriteOperation} from './plugin-api-types';
  import PluginWriteConfirmDialog, {type WriteConfirmResult} from './PluginWriteConfirmDialog.svelte';

  /**
   * Runs one plugin document in the sandboxed iframe and answers its API calls. Shared by the run
   * view (`live`) and the studio (`preview`); the write mode is the only behavioural difference.
   * The caller owns `docUrl` (see createPluginDocUrl) and revokes it.
   */
  interface Props {
    /** Keys the plugin's storage; any stable id works for a draft. */
    pluginId: string;
    pluginName: string;
    docUrl: string;
    /** Parsed from the same HTML `docUrl` was built from. */
    manifest: PluginManifest;
    writeMode: PluginWriteMode;
    entryId?: string;
    /** Bump to restart the plugin. */
    reloadToken?: number;
    /** Live only: skip the write dialog and just announce each write. */
    writesTrusted?: boolean;
    /** Live only: the user picked "Always allow" in the write dialog. */
    onTrustWrites?: () => void;
    /** Shown under the "Plugin stopped" message, e.g. a way back. */
    stoppedActions?: Snippet;
  }

  const {
    pluginId,
    pluginName,
    docUrl,
    manifest,
    writeMode,
    entryId,
    reloadToken = 0,
    writesTrusted = false,
    onTrustWrites,
    stoppedActions,
  }: Props = $props();

  const projectContext = useProjectContext();
  const features = useFeatures();
  const {base} = useRouter();

  function describeOperation(operation: PluginWriteOperation): string {
    switch (operation.kind) {
      case 'createEntry': return $t`“${pluginName}” added an entry`;
      case 'updateEntry': return $t`“${pluginName}” changed an entry`;
      case 'batch': return $t`“${pluginName}” made ${operation.count} changes`;
    }
  }

  // Write approvals are serialized so a plugin firing several writes shows one dialog at a time.
  let pendingWrite = $state<{operation: PluginWriteOperation; resolve: (approved: boolean) => void}>();
  let confirmQueue: Promise<unknown> = Promise.resolve();
  function confirmWrite(operation: PluginWriteOperation): Promise<boolean> {
    const result = confirmQueue.then(() => new Promise<boolean>(resolve => {
      if (writesTrusted && writeMode === 'live') {
        // Trusted plugins skip the dialog, but every write still surfaces — ambient disclosure
        // is what keeps "Always allow" honest.
        AppNotification.display(describeOperation(operation), {timeout: 'long'});
        resolve(true);
        return;
      }
      pendingWrite = {operation, resolve};
    }));
    confirmQueue = result;
    return result;
  }

  function resolvePendingWrite(result: WriteConfirmResult) {
    const pending = pendingWrite;
    if (!pending) return;
    pendingWrite = undefined;
    if (result === 'always') onTrustWrites?.();
    pending.resolve(result !== 'deny');
  }

  // 'view'/'edit' open a dialog over the still-mounted plugin; 'window' is offered only where the
  // platform supports separate windows (not Android/iOS), and degrades to the 'view' dialog there.
  const multiWindowService = useMultiWindowService();
  const openEntryModes: OpenEntryMode[] = ['view', 'edit', 'navigate', ...(multiWindowService ? ['window' as const] : [])];

  let entryDialogOpen = $state(false);
  let entryDialogId = $state<string>();
  let entryDialogMode = $state<'view' | 'edit'>('view');
  function openEntryDialog(id: string, mode: 'view' | 'edit') {
    entryDialogId = id;
    entryDialogMode = mode;
    entryDialogOpen = true;
  }
  function openEntry(id: string, mode: OpenEntryMode) {
    if (mode === 'navigate') {
      navigate(`${$base.uri}/browse?${entryBrowseParams(id)}`);
    } else if (mode === 'window' && multiWindowService) {
      void multiWindowService.openEntryInNewWindow(id);
    } else {
      // 'view', 'edit', or 'window' where new windows aren't supported.
      openEntryDialog(id, mode === 'edit' ? 'edit' : 'view');
    }
  }

  const host = $derived.by(() => {
    const adapter = new PluginApiAdapter(
      projectContext.api,
      // A preview leaves no saved state behind: its storage lives as long as this host.
      new PluginStorage(projectContext.projectCode, pluginId, writeMode === 'preview' ? createMemoryStorageBackend() : undefined),
      {
        permissions: manifest.permissions,
        capabilities: {comments: !!features.comments, history: !!features.history},
        writeMode,
      },
      {
        confirmWrite,
        openEntry,
        notify: (message) => AppNotification.display(message, {timeout: 'long'}),
      },
      projectContext.historyService,
    );
    return new PluginHost(adapter, {
      projectName: projectContext.projectName,
      projectCode: projectContext.projectCode,
      permissions: manifest.permissions,
      capabilities: {openEntryModes, comments: !!features.comments, history: !!features.history},
      entryId,
    });
  });

  let iframeElement = $state<HTMLIFrameElement>();
  $effect(() => {
    const attached = host;
    if (!iframeElement) return;
    attached.attach(iframeElement);
    return () => attached.detach();
  });

  /**
   * No sandbox flag or CSP directive stops a script from navigating ITS OWN frame — the one hole
   * in the offline sandbox, since a navigation's URL can carry data out. A plugin document never
   * legitimately navigates (it's a single self-contained file), so a second `load` on the iframe
   * means the plugin left its document: stop it and tell the user. Detection, not prevention —
   * which is why the consent card words the network guarantee as "not allowed" rather than "can't".
   */
  let iframeLoadCount = 0;
  let stoppedForNavigation = $state(false);
  function onIframeLoad() {
    iframeLoadCount++;
    if (iframeLoadCount > 1) stoppedForNavigation = true;
  }
  $effect(() => {
    reloadToken; docUrl; // eslint-disable-line @typescript-eslint/no-unused-expressions
    iframeLoadCount = 0;
    stoppedForNavigation = false;
  });
</script>

<svelte:window onmessage={(event) => host.handleWindowMessage(event)} />

{#if stoppedForNavigation}
  <div class="absolute inset-0 grid place-items-center p-4">
    <div class="text-center space-y-3 max-w-md">
      <Icon icon="i-mdi-shield-alert" class="size-10 text-destructive" />
      <div class="font-medium">{$t`Plugin stopped`}</div>
      <p class="text-sm text-muted-foreground">
        {$t`“${pluginName}” tried to leave its sandbox page, which plugins are not allowed to do. It may be broken — or trying to send data somewhere. Be careful with this plugin.`}
      </p>
      {@render stoppedActions?.()}
    </div>
  </div>
{:else}
  {#key reloadToken}
    <iframe
      bind:this={iframeElement}
      title={pluginName}
      src={docUrl}
      sandbox={PLUGIN_IFRAME_SANDBOX}
      allow={PLUGIN_IFRAME_ALLOW}
      onload={onIframeLoad}
      class="absolute inset-0 w-full h-full border-0 bg-background"
    ></iframe>
  {/key}
{/if}

<PluginWriteConfirmDialog
  {pluginName}
  {writeMode}
  operation={pendingWrite?.operation}
  onResult={resolvePendingWrite}
/>

<!-- Sibling overlay: opens over the plugin without unmounting its iframe, so plugin state survives. -->
<EditEntryDialog bind:open={entryDialogOpen} entryId={entryDialogId} mode={entryDialogMode} />
