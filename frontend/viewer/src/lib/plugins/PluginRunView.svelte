<script lang="ts">
  import * as Card from '$lib/components/ui/card';
  import {Badge} from '$lib/components/ui/badge';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {navigate, useRouter} from 'svelte-routing';
  import {usePluginService} from '$project/data/plugin-service.svelte';
  import {useProjectContext} from '$project/project-context.svelte';
  import {AppNotification} from '$lib/notifications/notifications';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import EditEntryDialog from '$lib/entry-editor/EditEntryDialog.svelte';
  import {PluginApiAdapter} from './plugin-api-adapter';
  import {PluginHost} from './plugin-host';
  import {buildPluginSrcdoc, parsePluginPermissions, PLUGIN_IFRAME_ALLOW, PLUGIN_IFRAME_SANDBOX} from './plugin-srcdoc';
  import {computePluginHash, PluginConsentStore, PluginStorage} from './plugin-local-data';
  import type {OpenEntryMode, PluginWriteOperation} from './plugin-api-types';
  import PluginWriteConfirmDialog from './PluginWriteConfirmDialog.svelte';

  interface Props {
    pluginId: string;
  }

  const {pluginId}: Props = $props();

  const launchEntryIdParam = new URLSearchParams(window.location.search).get('entryId') ?? undefined;
  const launchEntryId = launchEntryIdParam && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(launchEntryIdParam)
    ? launchEntryIdParam
    : undefined;

  const pluginService = usePluginService();
  const projectContext = useProjectContext();
  const {base} = useRouter();

  const plugin = $derived(pluginService.current.find(candidate => candidate.id === pluginId));
  const permissions = $derived(plugin ? parsePluginPermissions(plugin.html) : []);

  const consentStore = new PluginConsentStore(projectContext.projectCode);
  let contentHash = $state<string>();
  let approved = $state<boolean>();
  $effect(() => {
    const html = plugin?.html;
    if (html === undefined || !plugin) return;
    approved = undefined;
    void computePluginHash(html).then(hash => {
      contentHash = hash;
      approved = consentStore.isApproved(pluginId, hash);
    });
  });

  function approve() {
    if (!contentHash) return;
    consentStore.approve(pluginId, contentHash);
    approved = true;
  }

  // Write approvals are serialized so a plugin firing several writes shows one dialog at a time.
  let pendingWrite = $state<{operation: PluginWriteOperation; resolve: (approved: boolean) => void}>();
  let confirmQueue: Promise<unknown> = Promise.resolve();
  function confirmWrite(operation: PluginWriteOperation): Promise<boolean> {
    const result = confirmQueue.then(() => new Promise<boolean>(resolve => pendingWrite = {operation, resolve}));
    confirmQueue = result;
    return result;
  }

  function resolvePendingWrite(isApproved: boolean) {
    const pending = pendingWrite;
    if (!pending) return;
    pendingWrite = undefined;
    pending.resolve(isApproved);
  }

  // 'view'/'edit' open a dialog over the still-mounted plugin; 'window' is offered only where the
  // platform supports separate windows (not Android/iOS), and degrades to the 'view' dialog there.
  const multiWindowService = useMultiWindowService();
  const openEntryModes: OpenEntryMode[] = ['view', 'edit', 'navigate', ...(multiWindowService ? ['window' as const] : [])];

  let entryDialogOpen = $state(false);
  let entryDialogId = $state<string>();
  let entryDialogMode = $state<'view' | 'edit'>('view');
  function openEntryDialog(entryId: string, mode: 'view' | 'edit') {
    entryDialogId = entryId;
    entryDialogMode = mode;
    entryDialogOpen = true;
  }
  function openEntry(entryId: string, mode: OpenEntryMode) {
    if (mode === 'navigate') {
      navigate(`${$base.uri}/browse?${entryBrowseParams(entryId)}`);
    } else if (mode === 'window' && multiWindowService) {
      void multiWindowService.openEntryInNewWindow(entryId);
    } else {
      // 'view', 'edit', or 'window' where new windows aren't supported.
      openEntryDialog(entryId, mode === 'edit' ? 'edit' : 'view');
    }
  }

  const host = $derived.by(() => {
    if (!plugin) return undefined;
    const adapter = new PluginApiAdapter(
      projectContext.api,
      new PluginStorage(projectContext.projectCode, plugin.id),
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
      permissions,
      openEntryModes,
      entryId: launchEntryId,
    });
  });

  let iframeElement = $state<HTMLIFrameElement>();
  $effect(() => {
    if (!host || !iframeElement) return;
    host.attach(iframeElement);
    return () => host.detach();
  });

  const srcdoc = $derived(plugin && approved ? buildPluginSrcdoc(plugin.html, permissions) : undefined);
  let reloadToken = $state(0);

  // Fullscreen hides the whole toolbar for true immersion; the only chrome left is a minimal exit
  // control overlaid on the plugin (Esc also exits, browser-native). Feature-detected so we don't
  // offer a dead button where the Fullscreen API is unavailable (e.g. the Android WebView).
  const canFullscreen = document.fullscreenEnabled;
  let containerElement = $state<HTMLElement>();
  let isFullscreen = $state(false);
  function enterFullscreen() {
    void containerElement?.requestFullscreen();
  }
  function exitFullscreen() {
    if (document.fullscreenElement) void document.exitFullscreen();
  }
  function onFullscreenChange() {
    const nowFullscreen = !!document.fullscreenElement;
    if (nowFullscreen && !isFullscreen) AppNotification.display($t`Press Esc to exit fullscreen`, {timeout: 'short'});
    isFullscreen = nowFullscreen;
  }

  function goBack() {
    navigate(`${$base.uri}/plugins`);
  }
</script>

<svelte:window onmessage={(event) => host?.handleWindowMessage(event)} />
<svelte:document onfullscreenchange={onFullscreenChange} />

<div class="h-full flex flex-col bg-background" bind:this={containerElement}>
  {#if !isFullscreen}
    <div class="flex items-center gap-2 border-b px-2 py-1.5">
      <Button variant="ghost" size="icon" icon="i-mdi-arrow-left" title={$t`Back to plugins`} onclick={goBack} />
      <Icon icon="i-mdi-puzzle" class="text-primary" />
      <span class="font-medium truncate">{plugin?.name ?? $t`Plugin`}</span>
      {#if permissions.includes('internet')}
        <Badge variant="destructive">
          <Icon icon="i-mdi-web" />
          {$t`Internet`}
        </Badge>
      {/if}
      <div class="grow"></div>
      {#if approved}
        <Button variant="ghost" size="icon" icon="i-mdi-refresh" title={$t`Reload plugin`} onclick={() => reloadToken++} />
        {#if canFullscreen}
          <Button variant="ghost" size="icon" icon="i-mdi-fullscreen" title={$t`Fullscreen`} aria-label={$t`Fullscreen`} onclick={enterFullscreen} />
        {/if}
      {/if}
    </div>
  {/if}

  <div class="grow relative min-h-0">
    {#if !plugin}
      {#if !pluginService.loaded}
        <div class="absolute inset-0 grid place-items-center text-muted-foreground">
          <Icon icon="i-mdi-loading" class="animate-spin size-8" />
        </div>
      {:else}
        <div class="absolute inset-0 grid place-items-center">
          <div class="text-center space-y-3">
            <Icon icon="i-mdi-puzzle-remove" class="size-10 text-muted-foreground" />
            <div>{$t`This plugin no longer exists.`}</div>
            <Button variant="outline" onclick={goBack}>{$t`Back to plugins`}</Button>
          </div>
        </div>
      {/if}
    {:else if approved === false}
      <div class="absolute inset-0 grid place-items-center p-4 overflow-y-auto">
        <Card.Root class="max-w-md">
          <Card.Header>
            <Card.Title class="flex items-center gap-2">
              <Icon icon="i-mdi-shield-alert-outline" class="text-amber-500" />
              {$t`Run this plugin?`}
            </Card.Title>
            <Card.Description>{plugin.name}</Card.Description>
          </Card.Header>
          <Card.Content class="space-y-3 text-sm">
            <p>{$t`Plugins are code written by people on your team (often with AI help). This one hasn't run on this device yet, or it changed since it last ran.`}</p>
            <ul class="space-y-1">
              <li class="flex items-center gap-2">
                <Icon icon="i-mdi-check" class="text-green-600" />
                {$t`Runs in a sandbox, separate from the app`}
              </li>
              <li class="flex items-center gap-2">
                <Icon icon="i-mdi-check" class="text-green-600" />
                {$t`Every change to your dictionary needs your approval`}
              </li>
              <li class="flex items-center gap-2">
                {#if permissions.includes('internet')}
                  <Icon icon="i-mdi-alert" class="text-amber-500" />
                  {$t`Can access the internet (and could send dictionary data there)`}
                {:else}
                  <Icon icon="i-mdi-check" class="text-green-600" />
                  {$t`No internet access`}
                {/if}
              </li>
            </ul>
          </Card.Content>
          <Card.Footer class="gap-2 justify-end">
            <Button variant="outline" onclick={goBack}>{$t`Go back`}</Button>
            <Button icon="i-mdi-play" onclick={approve}>{$t`Run plugin`}</Button>
          </Card.Footer>
        </Card.Root>
      </div>
    {:else if srcdoc}
      {#key reloadToken}
        <iframe
          bind:this={iframeElement}
          title={plugin.name}
          {srcdoc}
          sandbox={PLUGIN_IFRAME_SANDBOX}
          allow={PLUGIN_IFRAME_ALLOW}
          class="absolute inset-0 w-full h-full border-0 bg-background"
        ></iframe>
      {/key}
      {#if isFullscreen}
        <!-- The plugin owns a sandboxed iframe, so pointer events don't reach the host: an
             auto-hiding-on-mouse-move control can't work here. Keep it persistent but faded. -->
        <Button
          variant="secondary"
          size="icon"
          icon="i-mdi-fullscreen-exit"
          title={$t`Exit fullscreen`}
          aria-label={$t`Exit fullscreen`}
          onclick={exitFullscreen}
          class="absolute top-2 end-2 z-10 opacity-40 shadow-md transition-opacity hover:opacity-100 focus-visible:opacity-100 motion-reduce:transition-none"
        />
      {/if}
    {/if}
  </div>
</div>

<PluginWriteConfirmDialog
  pluginName={plugin?.name ?? ''}
  operation={pendingWrite?.operation}
  onResult={resolvePendingWrite}
/>

<!-- Sibling overlay: opens over the plugin without unmounting its iframe, so plugin state survives. -->
<EditEntryDialog bind:open={entryDialogOpen} entryId={entryDialogId} mode={entryDialogMode} />
