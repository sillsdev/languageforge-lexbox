<script lang="ts">
  import type {Snippet} from 'svelte';
  import {on} from 'svelte/events';
  import {Icon} from '$lib/components/ui/icon';
  import {plural, t} from 'svelte-i18n-lingui';
  import {navigate, useRouter} from 'svelte-routing';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import EditEntryDialog from '$lib/entry-editor/EditEntryDialog.svelte';
  import {PluginApiAdapter} from './plugin-api-adapter';
  import {PluginHost} from './plugin-host';
  import {installPluginFramePolicy, PLUGIN_IFRAME_SANDBOX} from './plugin-srcdoc';
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
      case 'batch': return $t`“${pluginName}” made ${$plural(operation.count, {one: '# change', other: '# changes'})}`;
    }
  }

  // Write approvals are serialized so a plugin firing several writes shows one dialog at a time.
  let pendingWrite = $state<{operation: PluginWriteOperation; resolve: (approved: boolean) => void}>();
  let confirmQueue: Promise<unknown> = Promise.resolve();
  function confirmWrite(operation: PluginWriteOperation): Promise<boolean> {
    const result = confirmQueue.then(() => new Promise<boolean>(resolve => {
      if (stopped) {
        resolve(false);
        return;
      }
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

  /** A plugin that keeps asking after being refused is holding the user hostage behind a modal. */
  const MAX_DENIALS_IN_A_ROW = 3;
  let denialsInARow = 0;
  function resolvePendingWrite(result: WriteConfirmResult) {
    const pending = pendingWrite;
    if (!pending) return;
    pendingWrite = undefined;
    if (result === 'always') onTrustWrites?.();
    denialsInARow = result === 'deny' ? denialsInARow + 1 : 0;
    if (denialsInARow >= MAX_DENIALS_IN_A_ROW) stopped = 'declined';
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

  installPluginFramePolicy();

  /**
   * A plugin document never legitimately navigates (it's a single self-contained file), so a
   * plugin that tries is stopped and the user told. The page's frame-src blocks a navigation to
   * another origin and reports it as a violation (the plugin's is the only iframe on the page).
   * The blob:/data: documents that policy has to allow show up as a second `load`, which is best
   * effort: it misses a navigation that starts before the first `load`. WebRTC is outside CSP
   * altogether, which is why the consent card says "not allowed" to use the internet, not "can't".
   */
  let iframeLoadCount = 0;
  let stopped = $state<'navigation' | 'declined'>();
  function onIframeLoad() {
    iframeLoadCount++;
    if (iframeLoadCount > 1) stopped = 'navigation';
  }
  $effect(() => on(document, 'securitypolicyviolation', (event) => {
    if (event.effectiveDirective === 'frame-src') stopped = 'navigation';
  }));
  $effect(() => {
    reloadToken; docUrl; // eslint-disable-line @typescript-eslint/no-unused-expressions
    iframeLoadCount = 0;
    denialsInARow = 0;
    stopped = undefined;
  });
</script>

<svelte:window onmessage={(event) => host.handleWindowMessage(event)} />

{#if stopped}
  <div class="absolute inset-0 grid place-items-center p-4">
    <div class="text-center space-y-3 max-w-md">
      <Icon icon="i-mdi-shield-alert" class="size-10 text-destructive" />
      <div class="font-medium">{$t`Plugin stopped`}</div>
      <p class="text-sm text-muted-foreground">
        {#if stopped === 'navigation'}
          {$t`“${pluginName}” tried to leave its sandbox page, which plugins are not allowed to do. It may be broken — or trying to send data somewhere. Be careful with this plugin.`}
        {:else}
          {$t`“${pluginName}” kept asking to change your dictionary after you said no. Reload it to try again.`}
        {/if}
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
