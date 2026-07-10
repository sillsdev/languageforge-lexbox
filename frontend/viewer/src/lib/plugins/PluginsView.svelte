<script lang="ts">
  import * as Card from '$lib/components/ui/card';
  import {Alert, AlertDescription} from '$lib/components/ui/alert';
  import {Badge} from '$lib/components/ui/badge';
  import {Button, buttonVariants} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import * as DropdownMenu from '$lib/components/ui/dropdown-menu';
  import {t} from 'svelte-i18n-lingui';
  import {navigate, useRouter} from 'svelte-routing';
  import {type IPlugin, UserProjectRole} from '$lib/dotnet-types';
  import {usePluginService} from '$project/data/plugin-service.svelte';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import PluginEditorDialog from './PluginEditorDialog.svelte';
  import PluginAiPromptDialog from './PluginAiPromptDialog.svelte';

  const pluginService = usePluginService();
  const projectContext = useProjectContext();
  const dialogsService = useDialogsService();
  const {base} = useRouter();

  const plugins = $derived(pluginService.current);
  // Local-only projects have no role info; there the user owns their data anyway.
  const canManage = $derived(!projectContext.projectData || projectContext.projectData.role === UserProjectRole.Manager);

  let editorOpen = $state(false);
  let editingPlugin = $state<IPlugin | undefined>(undefined);
  let aiPromptOpen = $state(false);

  // The trust store is plain localStorage; bump this to re-read it after revoking from a card.
  let trustVersion = $state(0);
  // Trust is pinned to a content hash we'd need the file to verify, so the card shows whether a
  // grant EXISTS — if the plugin changed since, the grant is stale and it will ask again anyway.
  function hasWriteTrust(plugin: IPlugin): boolean {
    trustVersion; // eslint-disable-line @typescript-eslint/no-unused-expressions
    return pluginService.writeTrustStore.grantedHash(plugin.id) !== undefined;
  }

  function revokeWriteTrust(plugin: IPlugin) {
    pluginService.writeTrustStore.revoke(plugin.id);
    trustVersion++;
    AppNotification.display($t`“${plugin.name}” will ask before making changes again.`, {timeout: 'short'});
  }

  function run(plugin: IPlugin) {
    navigate(`${$base.uri}/plugins/${plugin.id}`);
  }

  function openCreate() {
    editingPlugin = undefined;
    editorOpen = true;
  }

  function openEdit(plugin: IPlugin) {
    editingPlugin = structuredClone($state.snapshot(plugin));
    editorOpen = true;
  }

  async function withPluginHtml(plugin: IPlugin, action: (html: string) => Promise<void> | void) {
    const loaded = await pluginService.getHtml(plugin);
    if (loaded.result === 'offline') {
      AppNotification.display($t`You're offline and this plugin's file isn't on this device yet.`, 'warning');
      return;
    }
    if (loaded.result === 'error') {
      AppNotification.error($t`Couldn't load the plugin file. ${loaded.message ?? ''}`);
      return;
    }
    await action(loaded.html);
  }

  function exportPlugin(plugin: IPlugin) {
    return withPluginHtml(plugin, (html) => {
      const url = URL.createObjectURL(new Blob([html], {type: 'text/html'}));
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = pluginFileName(plugin.name);
      anchor.click();
      URL.revokeObjectURL(url);
    });
  }

  function pluginFileName(name: string): string {
    const safe = name.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '');
    return `${safe || 'plugin'}.html`;
  }

  function duplicate(plugin: IPlugin) {
    return withPluginHtml(plugin, async (html) => {
      await pluginService.create({name: $t`${plugin.name} (copy)`, description: plugin.description, html});
    });
  }

  async function onDelete(plugin: IPlugin) {
    const shouldDelete = await dialogsService.promptDelete($t`Plugin`, plugin.name, {
      isDangerous: true,
      details: $t`This removes the plugin for everyone on this project.`,
    });
    if (!shouldDelete) return;
    await pluginService.delete(plugin.id);
  }

  function sizeKb(plugin: IPlugin): string {
    return `${Math.max(1, Math.round(plugin.fileSize / 1024))} KB`;
  }
</script>

<div class="h-full overflow-y-auto">
  <div class="max-w-4xl mx-auto p-4 md:p-6 space-y-4">
    <div class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-2xl font-semibold flex items-center gap-2">
          <Icon icon="i-mdi-puzzle" class="text-primary" />
          {$t`Plugins`}
        </h1>
        <p class="text-sm text-muted-foreground">
          {$t`Small custom apps that work with this project's dictionary — shared with the whole team.`}
        </p>
      </div>
      <div class="flex gap-2">
        <Button variant="outline" icon="i-mdi-creation" onclick={() => aiPromptOpen = true}>
          {$t`Get AI prompt`}
        </Button>
        {#if canManage}
          <Button icon="i-mdi-plus" onclick={openCreate}>
            {$t`New plugin`}
          </Button>
        {/if}
      </div>
    </div>

    <Alert>
      <Icon icon="i-mdi-shield-alert-outline" class="text-amber-500" />
      <AlertDescription>
        {$t`Plugins are code. They run in a protected sandbox and can only change dictionary data with your approval, but you should still only use plugins from people you trust.`}
      </AlertDescription>
    </Alert>

    {#if plugins.length === 0}
      <Card.Root>
        <Card.Content class="py-10 text-center space-y-3">
          {#if pluginService.loading}
            <Icon icon="i-mdi-loading" class="animate-spin size-8 text-muted-foreground" />
          {:else}
            <Icon icon="i-mdi-puzzle-outline" class="size-10 text-muted-foreground" />
            <div class="font-medium">{$t`No plugins yet`}</div>
            <p class="text-sm text-muted-foreground max-w-md mx-auto">
              {$t`Ask an AI to build one for you — click “Get AI prompt”, describe your idea, and paste the result into “New plugin”. Or start from a built-in example.`}
            </p>
            {#if canManage}
              <Button icon="i-mdi-plus" onclick={openCreate}>{$t`New plugin`}</Button>
            {/if}
          {/if}
        </Card.Content>
      </Card.Root>
    {:else}
      <div class="grid gap-3 sm:grid-cols-2">
        {#each plugins as plugin (plugin.id)}
          <Card.Root class="hover:border-primary/50 transition-colors">
            <Card.Header>
              <Card.Title class="flex items-center gap-2 min-w-0">
                <Icon icon="i-mdi-puzzle" class="text-primary shrink-0" />
                <span class="truncate">{plugin.name}</span>
              </Card.Title>
              {#if plugin.description?.trim()}
                <p class="text-sm text-muted-foreground line-clamp-2">{plugin.description}</p>
              {/if}
              <Card.Description class="flex flex-wrap items-center gap-2">
                <span>{sizeKb(plugin)}</span>
                {#if !plugin.permissions.includes('edit')}
                  <Badge variant="secondary">
                    <Icon icon="i-mdi-eye-outline" />
                    {$t`Read-only`}
                  </Badge>
                {:else if hasWriteTrust(plugin)}
                  <Badge variant="secondary">
                    <Icon icon="i-mdi-shield-check" />
                    {$t`Edits without asking`}
                  </Badge>
                {:else}
                  <Badge variant="secondary">
                    <Icon icon="i-mdi-pencil-outline" />
                    {$t`Edits — asks first`}
                  </Badge>
                {/if}
                {#if plugin.contexts.includes('entry')}
                  <Badge variant="secondary">
                    <Icon icon="i-mdi-menu" />
                    {$t`Entry menu`}
                  </Badge>
                {/if}
                {#if plugin.permissions.includes('internet')}
                  <Badge variant="destructive">
                    <Icon icon="i-mdi-web" />
                    {$t`Internet`}
                  </Badge>
                {/if}
              </Card.Description>
            </Card.Header>
            <Card.Footer class="gap-2">
              <Button icon="i-mdi-play" onclick={() => run(plugin)}>{$t`Run`}</Button>
              <div class="grow"></div>
              {#if canManage}
                <Button variant="ghost" size="icon" icon="i-mdi-pencil" title={$t`Edit`} onclick={() => openEdit(plugin)} />
              {/if}
              <DropdownMenu.Root>
                <DropdownMenu.Trigger class={buttonVariants({variant: 'ghost', size: 'icon'})} title={$t`More`}>
                  <Icon icon="i-mdi-dots-vertical" />
                </DropdownMenu.Trigger>
                <DropdownMenu.Content align="end">
                  <DropdownMenu.Item onSelect={() => void exportPlugin(plugin)}>
                    <Icon icon="i-mdi-download" />
                    {$t`Export`}
                  </DropdownMenu.Item>
                  {#if hasWriteTrust(plugin)}
                    <DropdownMenu.Item onSelect={() => revokeWriteTrust(plugin)}>
                      <Icon icon="i-mdi-shield-off-outline" />
                      {$t`Require approval for edits`}
                    </DropdownMenu.Item>
                  {/if}
                  {#if canManage}
                    <DropdownMenu.Item onSelect={() => void duplicate(plugin)}>
                      <Icon icon="i-mdi-content-copy" />
                      {$t`Duplicate`}
                    </DropdownMenu.Item>
                    <DropdownMenu.Separator />
                    <DropdownMenu.Item variant="destructive" onSelect={() => void onDelete(plugin)}>
                      <Icon icon="i-mdi-trash-can-outline" />
                      {$t`Delete`}
                    </DropdownMenu.Item>
                  {/if}
                </DropdownMenu.Content>
              </DropdownMenu.Root>
            </Card.Footer>
          </Card.Root>
        {/each}
      </div>
    {/if}
  </div>
</div>

<PluginEditorDialog bind:open={editorOpen} plugin={editingPlugin} />
<PluginAiPromptDialog bind:open={aiPromptOpen} />
