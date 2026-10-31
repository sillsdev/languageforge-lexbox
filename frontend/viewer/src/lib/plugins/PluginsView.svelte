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
  import type {IconClass} from '$lib/icon-class';
  import {
    exampleFunctions,
    examplePlugins,
    examplePluginsByPrimaryFunction,
    type ExampleCapability,
    type ExampleFunction,
    type ExamplePlugin,
  } from './examples';

  const pluginService = usePluginService();
  const projectContext = useProjectContext();
  const dialogsService = useDialogsService();
  const {base} = useRouter();

  const plugins = $derived(pluginService.current);
  // Local-only projects have no role info; there the user owns their data anyway.
  const canManage = $derived(!projectContext.projectData || projectContext.projectData.role === UserProjectRole.Manager);

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
    navigate(`${$base.uri}/plugins/new`);
  }

  function openEdit(plugin: IPlugin) {
    navigate(`${$base.uri}/plugins/${plugin.id}/edit`);
  }

  function tryExample(example: ExamplePlugin) {
    navigate(`${$base.uri}/plugins/examples/${example.key}`);
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

  // ---- Examples shelf: managers only, since trying an example is the first step to adding it ----

  let shelfFilter = $state<ExampleFunction | 'all'>('all');
  let showAllExamples = $state(false);
  // Unfiltered and collapsed, the shelf shows one example per function as a taste of the range.
  const sampler = examplePluginsByPrimaryFunction().map(group => group.plugins[0]);
  const shelfExamples = $derived.by(() => {
    const filter = shelfFilter;
    if (filter !== 'all') return examplePlugins.filter(example => example.functions.includes(filter));
    return showAllExamples ? examplePlugins : sampler;
  });

  const functionLabels: Record<ExampleFunction, string> = $derived({
    collect: $t`Collect words`,
    enrich: $t`Enrich & clean up`,
    record: $t`Record & illustrate`,
    explore: $t`Explore & analyze`,
    review: $t`Review & history`,
    publish: $t`Publish`,
    play: $t`Play & learn`,
  });

  const capabilityMeta: Record<ExampleCapability, {icon: IconClass; label: string; variant: 'destructive' | 'secondary'}> = $derived({
    internet: {icon: 'i-mdi-web', label: $t`Internet`, variant: 'destructive'},
    microphone: {icon: 'i-mdi-microphone', label: $t`Microphone`, variant: 'secondary'},
    camera: {icon: 'i-mdi-camera', label: $t`Camera`, variant: 'secondary'},
    'entry-menu': {icon: 'i-mdi-menu', label: $t`Entry menu`, variant: 'secondary'},
  });
</script>

<div class="h-full overflow-y-auto">
  <div class="max-w-5xl mx-auto p-4 md:p-6 space-y-6">
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
      {#if canManage}
        <Button icon="i-mdi-plus" onclick={openCreate}>
          {$t`New plugin`}
        </Button>
      {/if}
    </div>

    <Alert>
      <Icon icon="i-mdi-shield-alert-outline" class="text-amber-500" />
      <AlertDescription>
        {$t`Plugins are code. They run in a protected sandbox and can only change dictionary data with your approval, but you should still only use plugins from people you trust.`}
      </AlertDescription>
    </Alert>

    <section aria-labelledby="plugins-in-project" class="space-y-2">
      <h2 id="plugins-in-project" class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
        {$t`In this project`}
      </h2>
      {#if plugins.length === 0}
        <Card.Root>
          <Card.Content class="py-8 text-center space-y-3">
            {#if pluginService.loading}
              <Icon icon="i-mdi-loading" class="animate-spin size-8 text-muted-foreground" />
            {:else}
              <Icon icon="i-mdi-puzzle-outline" class="size-10 text-muted-foreground" />
              <div class="font-medium">{$t`No plugins yet`}</div>
              <p class="text-sm text-muted-foreground max-w-md mx-auto">
                {#if canManage}
                  {$t`Try one of the examples below on your own data, or make a new plugin with AI help.`}
                {:else}
                  {$t`A project manager can add plugins for the team.`}
                {/if}
              </p>
            {/if}
          </Card.Content>
        </Card.Root>
      {:else}
        <div class="grid gap-3 sm:grid-cols-2">
          {#each plugins as plugin (plugin.id)}
            <Card.Root class="hover:border-primary/50 transition-colors" data-testid="project-plugin-card">
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
                {#if canManage}
                  <Button variant="outline" icon="i-mdi-pencil" onclick={() => openEdit(plugin)}>{$t`Edit`}</Button>
                {/if}
                <div class="grow"></div>
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
    </section>

    {#if canManage}
      <section aria-labelledby="plugin-examples" class="space-y-3 rounded-xl bg-muted/60 p-4">
        <div>
          <h2 id="plugin-examples" class="font-semibold">{$t`Examples`}</h2>
          <p class="text-sm text-muted-foreground">
            {$t`Not in this project. Try one on your data, then add it or make it your own.`}
          </p>
        </div>
        <div class="flex flex-wrap items-center gap-1.5" role="group" aria-label={$t`Filter examples`}>
          {#each ['all' as const, ...exampleFunctions] as fn (fn)}
            {@const pressed = shelfFilter === fn}
            <button
              type="button"
              aria-pressed={pressed}
              class="rounded-full border px-3 py-1 text-xs transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring
                {pressed ? 'border-foreground bg-foreground text-background' : 'bg-background hover:bg-muted'}"
              onclick={() => shelfFilter = fn}>
              {fn === 'all' ? $t`All` : functionLabels[fn]}
            </button>
          {/each}
        </div>
        <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {#each shelfExamples as example (example.key)}
            <Card.Root class="gap-3 py-4" data-testid="plugin-example-card">
              <Card.Header class="px-4">
                <Card.Title class="text-base">{example.name}</Card.Title>
                <p class="text-sm text-muted-foreground line-clamp-3">{example.description}</p>
              </Card.Header>
              <Card.Footer class="mt-auto flex-wrap gap-2 px-4">
                <Button
                  variant="outline"
                  size="sm"
                  icon="i-mdi-play-outline"
                  aria-label={$t`Try ${example.name}`}
                  onclick={() => tryExample(example)}
                >
                  {$t`Try it`}
                </Button>
                <div class="grow"></div>
                {#each example.capabilities ?? [] as capability (capability)}
                  <Badge variant={capabilityMeta[capability].variant}>
                    <Icon icon={capabilityMeta[capability].icon} />
                    {capabilityMeta[capability].label}
                  </Badge>
                {/each}
              </Card.Footer>
            </Card.Root>
          {/each}
        </div>
        {#if shelfFilter === 'all'}
          <Button variant="link" class="px-0" onclick={() => showAllExamples = !showAllExamples}>
            {showAllExamples ? $t`Show fewer examples` : $t`See all ${examplePlugins.length} examples`}
          </Button>
        {/if}
      </section>
    {/if}
  </div>
</div>
