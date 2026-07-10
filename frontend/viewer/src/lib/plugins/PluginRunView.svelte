<script lang="ts">
  import * as Card from '$lib/components/ui/card';
  import {Badge} from '$lib/components/ui/badge';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {navigate, useRouter} from 'svelte-routing';
  import {usePluginService} from '$project/data/plugin-service.svelte';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useFeatures} from '$lib/services/feature-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import {entryBrowseParams} from '$lib/utils/search-params';
  import {useMultiWindowService} from '$lib/services/multi-window-service';
  import EditEntryDialog from '$lib/entry-editor/EditEntryDialog.svelte';
  import {PluginApiAdapter} from './plugin-api-adapter';
  import {PluginHost} from './plugin-host';
  import {buildPluginSrcdoc, PLUGIN_IFRAME_ALLOW, PLUGIN_IFRAME_SANDBOX} from './plugin-srcdoc';
  import {parsePluginManifest, type PluginManifest} from './plugin-manifest';
  import {computePluginHash, PluginStorage} from './plugin-local-data';
  import type {OpenEntryMode, PluginWriteOperation} from './plugin-api-types';
  import PluginWriteConfirmDialog, {type WriteConfirmResult} from './PluginWriteConfirmDialog.svelte';

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
  const features = useFeatures();
  const {base} = useRouter();

  const plugin = $derived(pluginService.current.find(candidate => candidate.id === pluginId));

  /**
   * The plugin's HTML lives in a media file (downloaded on demand, cached locally), so running has
   * an async load step with offline/error outcomes. Everything trust-related — the manifest, the
   * consent hash, the sandbox CSP — is derived from these actual bytes, never from entity metadata.
   */
  type HtmlState =
    | {status: 'loading'}
    | {status: 'offline'}
    | {status: 'error'; message?: string}
    | {status: 'ready'; html: string; manifest: PluginManifest; hash: string};
  let htmlState = $state<HtmlState>({status: 'loading'});

  $effect(() => {
    if (!plugin) return;
    const requested = plugin;
    htmlState = {status: 'loading'};
    void pluginService.getHtml(requested).then(async result => {
      if (plugin !== requested) return; // a newer load owns the state
      if (result.result === 'ok') {
        htmlState = {
          status: 'ready',
          html: result.html,
          manifest: parsePluginManifest(result.html),
          hash: await computePluginHash({name: requested.name, description: requested.description, html: result.html}),
        };
      } else if (result.result === 'offline') {
        htmlState = {status: 'offline'};
      } else {
        htmlState = {status: 'error', message: result.message};
      }
    });
  });

  const manifest = $derived(htmlState.status === 'ready' ? htmlState.manifest : undefined);
  const missingRequirements = $derived((manifest?.requires ?? []).filter(feature => !features[feature]));

  // Run consent + write trust are device-local and pinned to the content hash (see plugin-local-data).
  let approved = $state<boolean>();
  let writesTrusted = $state(false);
  $effect(() => {
    if (htmlState.status !== 'ready') {
      approved = undefined;
      return;
    }
    approved = pluginService.consentStore.isGranted(pluginId, htmlState.hash);
    writesTrusted = pluginService.writeTrustStore.isGranted(pluginId, htmlState.hash);
  });

  function approve() {
    if (htmlState.status !== 'ready') return;
    pluginService.consentStore.grant(pluginId, htmlState.hash);
    approved = true;
  }

  function trustWrites() {
    if (htmlState.status !== 'ready' || !plugin) return;
    pluginService.writeTrustStore.grant(pluginId, htmlState.hash);
    writesTrusted = true;
    AppNotification.display(
      $t`“${plugin.name}” can now make changes without asking. You can change this from the plugin toolbar or its card.`,
      {timeout: 'long'});
  }

  function revokeWriteTrust() {
    if (!plugin) return;
    pluginService.writeTrustStore.revoke(pluginId);
    writesTrusted = false;
    AppNotification.display($t`“${plugin.name}” will ask before making changes again.`, {timeout: 'short'});
  }

  function describeOperation(operation: PluginWriteOperation): string {
    const name = plugin?.name ?? $t`Plugin`;
    switch (operation.kind) {
      case 'createEntry': return $t`“${name}” added an entry`;
      case 'updateEntry': return $t`“${name}” changed an entry`;
      case 'batch': return $t`“${name}” made ${operation.count} changes`;
    }
  }

  // Write approvals are serialized so a plugin firing several writes shows one dialog at a time.
  let pendingWrite = $state<{operation: PluginWriteOperation; resolve: (approved: boolean) => void}>();
  let confirmQueue: Promise<unknown> = Promise.resolve();
  function confirmWrite(operation: PluginWriteOperation): Promise<boolean> {
    const result = confirmQueue.then(() => new Promise<boolean>(resolve => {
      if (writesTrusted) {
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
    if (result === 'always') trustWrites();
    pending.resolve(result !== 'deny');
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
    if (!plugin || !manifest) return undefined;
    const adapter = new PluginApiAdapter(
      projectContext.api,
      new PluginStorage(projectContext.projectCode, plugin.id),
      {
        permissions: manifest.permissions,
        capabilities: {comments: !!features.comments, history: !!features.history},
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
      entryId: launchEntryId,
    });
  });

  let iframeElement = $state<HTMLIFrameElement>();
  $effect(() => {
    if (!host || !iframeElement) return;
    host.attach(iframeElement);
    return () => host.detach();
  });

  const srcdoc = $derived(
    htmlState.status === 'ready' && approved ? buildPluginSrcdoc(htmlState.html, htmlState.manifest.permissions) : undefined);
  let reloadToken = $state(0);

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
    reloadToken; srcdoc; // eslint-disable-line @typescript-eslint/no-unused-expressions
    iframeLoadCount = 0;
    stoppedForNavigation = false;
  });

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
      {#if manifest?.permissions.includes('internet')}
        <Badge variant="destructive">
          <Icon icon="i-mdi-web" />
          {$t`Internet`}
        </Badge>
      {/if}
      <div class="grow"></div>
      {#if approved}
        {#if writesTrusted}
          <Button
            variant="ghost"
            size="sm"
            icon="i-mdi-shield-check"
            class="text-muted-foreground"
            title={$t`This plugin can make changes without asking. Click to make it ask again.`}
            onclick={revokeWriteTrust}
          >
            {$t`Trusted`}
          </Button>
        {/if}
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
    {:else if stoppedForNavigation}
      <div class="absolute inset-0 grid place-items-center p-4">
        <div class="text-center space-y-3 max-w-md">
          <Icon icon="i-mdi-shield-alert" class="size-10 text-destructive" />
          <div class="font-medium">{$t`Plugin stopped`}</div>
          <p class="text-sm text-muted-foreground">
            {$t`“${plugin.name}” tried to leave its sandbox page, which plugins are not allowed to do. It may be broken — or trying to send data somewhere. Be careful with this plugin.`}
          </p>
          <Button variant="outline" onclick={goBack}>{$t`Back to plugins`}</Button>
        </div>
      </div>
    {:else if htmlState.status === 'loading'}
      <div class="absolute inset-0 grid place-items-center text-muted-foreground">
        <Icon icon="i-mdi-loading" class="animate-spin size-8" />
      </div>
    {:else if htmlState.status === 'offline'}
      <div class="absolute inset-0 grid place-items-center p-4">
        <div class="text-center space-y-3 max-w-md">
          <Icon icon="i-mdi-cloud-off-outline" class="size-10 text-muted-foreground" />
          <div class="font-medium">{$t`This plugin isn't on this device yet`}</div>
          <p class="text-sm text-muted-foreground">
            {$t`You're offline and the plugin file hasn't been downloaded here. Connect to the internet and try again — after that it works offline.`}
          </p>
          <Button variant="outline" onclick={goBack}>{$t`Back to plugins`}</Button>
        </div>
      </div>
    {:else if htmlState.status === 'error'}
      <div class="absolute inset-0 grid place-items-center p-4">
        <div class="text-center space-y-3 max-w-md">
          <Icon icon="i-mdi-alert-circle-outline" class="size-10 text-destructive" />
          <div class="font-medium">{$t`Couldn't load this plugin`}</div>
          {#if htmlState.message}
            <p class="text-sm text-muted-foreground break-words">{htmlState.message}</p>
          {/if}
          <Button variant="outline" onclick={goBack}>{$t`Back to plugins`}</Button>
        </div>
      </div>
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
                {#if manifest?.permissions.includes('edit')}
                  <Icon icon="i-mdi-check" class="text-green-600" />
                  {$t`Can change your dictionary — it will ask you first`}
                {:else}
                  <Icon icon="i-mdi-check" class="text-green-600" />
                  {$t`Read-only: cannot change your dictionary`}
                {/if}
              </li>
              <li class="flex items-center gap-2">
                {#if manifest?.permissions.includes('internet')}
                  <Icon icon="i-mdi-alert" class="text-amber-500" />
                  {$t`Can access the internet (and could send dictionary data there)`}
                {:else}
                  <Icon icon="i-mdi-check" class="text-green-600" />
                  {$t`Not allowed to use the internet`}
                {/if}
              </li>
              {#each missingRequirements as feature (feature)}
                <li class="flex items-center gap-2">
                  <Icon icon="i-mdi-alert" class="text-amber-500" />
                  {$t`Needs “${feature}”, which this project doesn't support — it may not work properly`}
                </li>
              {/each}
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
          onload={onIframeLoad}
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
