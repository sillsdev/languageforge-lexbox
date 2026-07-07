<script lang="ts">
  import * as Card from '$lib/components/ui/card';
  import {Alert, AlertDescription} from '$lib/components/ui/alert';
  import {Badge} from '$lib/components/ui/badge';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {t} from 'svelte-i18n-lingui';
  import {navigate, useRouter} from 'svelte-routing';
  import {type IPlugin, UserProjectRole} from '$lib/dotnet-types';
  import {usePluginService} from '$project/data/plugin-service.svelte';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {parsePluginPermissions} from './plugin-srcdoc';
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

  async function onSubmit(plugin: IPlugin) {
    if (editingPlugin) await pluginService.update(plugin);
    else await pluginService.add(plugin);
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
    return `${Math.max(1, Math.round(plugin.html.length / 1024))} KB`;
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
              <Card.Description class="flex items-center gap-2">
                <span>{sizeKb(plugin)}</span>
                {#if parsePluginPermissions(plugin.html).includes('internet')}
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
                <Button
                  variant="ghost"
                  size="icon"
                  icon="i-mdi-trash-can-outline"
                  title={$t`Delete`}
                  class="hover:bg-destructive/20! hover:text-destructive"
                  onclick={() => void onDelete(plugin)}
                />
              {/if}
            </Card.Footer>
          </Card.Root>
        {/each}
      </div>
    {/if}
  </div>
</div>

<PluginEditorDialog bind:open={editorOpen} plugin={editingPlugin} {onSubmit} />
<PluginAiPromptDialog bind:open={aiPromptOpen} />
