<script lang="ts">
  import * as Dialog from '$lib/components/ui/dialog';
  import {Alert, AlertDescription} from '$lib/components/ui/alert';
  import {Badge} from '$lib/components/ui/badge';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Input} from '$lib/components/ui/input';
  import {Label} from '$lib/components/ui/label';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';
  import {tick} from 'svelte';
  import type {IPlugin} from '$lib/dotnet-types';
  import {parsePluginPermissions} from './plugin-srcdoc';
  import {
    exampleFunctionLabel,
    exampleFunctions,
    examplePlugins,
    examplePluginsByPrimaryFunction,
    type ExampleCapability,
    type ExampleFunction,
    type ExamplePlugin,
  } from './examples';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import type {IconClass} from '$lib/icon-class';

  interface Props {
    open: boolean;
    /** The plugin to edit; leave undefined to create a new one. */
    plugin?: IPlugin;
    onSubmit: (plugin: IPlugin) => Promise<void>;
  }

  let {open = $bindable(), plugin, onSubmit}: Props = $props();

  type View = 'form' | 'gallery';
  let view = $state<View>('form');

  useBackHandler({addToStack: () => open, onBack: () => open = false, key: 'plugin-editor-dialog'});
  // While the example gallery is open, back/Esc returns to the form rather than closing the dialog.
  useBackHandler({addToStack: () => open && view === 'gallery', onBack: () => view = 'form', key: 'plugin-example-gallery'});

  let name = $state('');
  let description = $state('');
  let html = $state('');
  let saving = $state(false);
  let loadingExample = $state<string>();
  let loadError = $state(false);
  let search = $state('');
  let selectedFunctions = $state<ExampleFunction[]>([]);
  let fileInput = $state<HTMLInputElement>();
  // Whether a field was filled from an example/file rather than typed. An auto-filled value keeps
  // following the selected example (so switching examples updates it); a typed value is left alone.
  let nameAutoFilled = $state(false);
  let descriptionAutoFilled = $state(false);

  $effect(() => {
    if (open) {
      name = plugin?.name ?? '';
      description = plugin?.description ?? '';
      html = plugin?.html ?? '';
      nameAutoFilled = false;
      descriptionAutoFilled = false;
      view = 'form';
      search = '';
      selectedFunctions = [];
      loadError = false;
    }
  });

  const permissions = $derived(html.trim() ? parsePluginPermissions(html) : []);
  const sizeKb = $derived(Math.round(html.length / 102.4) / 10);
  const canSave = $derived(!!name.trim() && !!html.trim() && !saving);

  const primaryGroups = examplePluginsByPrimaryFunction();

  function matchesSearch(example: ExamplePlugin): boolean {
    const term = search.trim().toLowerCase();
    return !term || example.name.toLowerCase().includes(term) || example.description.toLowerCase().includes(term);
  }

  // No chips → browse the curated sections (each example once). Any chip → a single flat grid of
  // everything matching the selected functions (OR), further narrowed by the search text (AND).
  const filtering = $derived(selectedFunctions.length > 0);
  const groupedView = $derived.by(() =>
    primaryGroups
      .map((group) => ({function: group.function, plugins: group.plugins.filter(matchesSearch)}))
      .filter((group) => group.plugins.length > 0));
  const flatResults = $derived.by(() =>
    examplePlugins.filter((example) =>
      example.functions.some((fn) => selectedFunctions.includes(fn)) && matchesSearch(example)));
  const shownCount = $derived(filtering ? flatResults.length : groupedView.reduce((n, g) => n + g.plugins.length, 0));
  const hasActiveFilter = $derived(filtering || !!search.trim());

  const capabilityMeta: Record<ExampleCapability, {icon: IconClass; label: string; variant: 'destructive' | 'secondary'}> = {
    internet: {icon: 'i-mdi-web', label: $t`Internet`, variant: 'destructive'},
    microphone: {icon: 'i-mdi-microphone', label: $t`Microphone`, variant: 'secondary'},
    camera: {icon: 'i-mdi-camera', label: $t`Camera`, variant: 'secondary'},
    'entry-menu': {icon: 'i-mdi-menu', label: $t`Entry menu`, variant: 'secondary'},
  };

  function toggleFunction(key: ExampleFunction) {
    selectedFunctions = selectedFunctions.includes(key)
      ? selectedFunctions.filter((k) => k !== key)
      : [...selectedFunctions, key];
  }

  async function clearFilters() {
    selectedFunctions = [];
    search = '';
    await tick();
    document.getElementById('plugin-example-search')?.focus();
  }

  function applyExampleValues(exampleName: string, exampleDescription: string | undefined, exampleHtml: string) {
    if (!name.trim() || nameAutoFilled) {
      name = exampleName;
      nameAutoFilled = true;
    }
    if (exampleDescription !== undefined && (!description.trim() || descriptionAutoFilled)) {
      description = exampleDescription;
      descriptionAutoFilled = true;
    }
    html = exampleHtml;
  }

  async function useExample(example: ExamplePlugin) {
    loadingExample = example.key;
    loadError = false;
    try {
      const exampleHtml = await example.loadHtml();
      applyExampleValues(example.name, example.description, exampleHtml);
      view = 'form';
      await tick();
      document.getElementById('plugin-name')?.focus();
    } catch {
      loadError = true;
    } finally {
      loadingExample = undefined;
    }
  }

  async function importFile(event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    applyExampleValues(file.name.replace(/\.html?$/i, ''), undefined, await file.text());
  }

  async function openGallery() {
    loadError = false;
    view = 'gallery';
    await tick();
    document.getElementById('plugin-example-search')?.focus();
  }

  async function save() {
    saving = true;
    try {
      await onSubmit({
        id: plugin?.id ?? crypto.randomUUID(),
        name: name.trim(),
        description: description.trim() || undefined,
        html,
      });
      open = false;
    } finally {
      saving = false;
    }
  }
</script>

{#snippet exampleCard(example: ExamplePlugin, showPrimaryFunction: boolean)}
  <button
    type="button"
    class="group relative flex flex-col items-start gap-1 rounded-lg border p-3 text-left transition-colors hover:border-primary/50 hover:bg-muted/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
    disabled={!!loadingExample}
    aria-describedby="desc-{example.key}"
    onclick={() => void useExample(example)}>
    {#if showPrimaryFunction}
      <span class="text-xs font-medium uppercase tracking-wide text-muted-foreground">
        {exampleFunctionLabel(example.functions[0])}
      </span>
    {/if}
    <span class="font-medium">{example.name}</span>
    <span id="desc-{example.key}" class="text-sm text-muted-foreground line-clamp-2">{example.description}</span>
    {#if example.capabilities?.length}
      <div class="flex flex-wrap gap-1 pt-1">
        {#each example.capabilities as capability (capability)}
          <Badge variant={capabilityMeta[capability].variant}>
            <Icon icon={capabilityMeta[capability].icon} />
            {capabilityMeta[capability].label}
          </Badge>
        {/each}
      </div>
    {/if}
    {#if loadingExample === example.key}
      <div class="absolute inset-0 grid place-items-center rounded-lg bg-background/60">
        <Icon icon="i-mdi-loading" class="animate-spin size-6 text-primary" />
      </div>
    {/if}
  </button>
{/snippet}

<Dialog.Root bind:open>
  <Dialog.Content class="max-w-3xl" interactOutsideBehavior="ignore">
    {#if view === 'gallery'}
      <Dialog.Header>
        <div class="flex items-center gap-2">
          <Button variant="ghost" size="icon" icon="i-mdi-arrow-left" title={$t`Back`} onclick={() => view = 'form'} />
          <div class="min-w-0">
            <Dialog.Title>{$t`Start from an example`}</Dialog.Title>
            <Dialog.Description>{$t`Pick a starting point — you can edit everything afterwards.`}</Dialog.Description>
          </div>
          <div class="grow"></div>
          <div class="relative">
            <Icon icon="i-mdi-magnify" class="absolute left-2 top-1/2 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="plugin-example-search"
              bind:value={search}
              placeholder={$t`Search examples`}
              aria-label={$t`Search examples`}
              class="w-48 pl-8"
            />
          </div>
        </div>
      </Dialog.Header>

      <div class="flex flex-wrap items-center gap-1.5" role="group" aria-label={$t`Filter by function`}>
        {#each exampleFunctions as fn (fn.key)}
          {@const pressed = selectedFunctions.includes(fn.key)}
          <button
            type="button"
            aria-pressed={pressed}
            class="rounded-full border px-3 py-1 text-xs transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring
              {pressed ? 'border-primary bg-primary text-primary-foreground' : 'bg-background hover:bg-muted'}"
            onclick={() => toggleFunction(fn.key)}>
            {fn.label}
          </button>
        {/each}
        {#if hasActiveFilter}
          <Button variant="ghost" size="xs" onclick={() => void clearFilters()}>{$t`Clear`}</Button>
        {/if}
      </div>

      <div class="sr-only" role="status" aria-live="polite">
        {#if hasActiveFilter}
          {$t`Showing ${shownCount} examples`}
        {:else}
          {$t`Showing all ${examplePlugins.length} examples in ${primaryGroups.length} categories`}
        {/if}
      </div>

      <div class="max-h-[60vh] overflow-y-auto pr-1 -mr-1 space-y-5">
        {#if shownCount === 0}
          <div class="py-10 text-center space-y-3">
            <p class="text-sm text-muted-foreground">{$t`No examples match your filters.`}</p>
            <Button variant="outline" size="sm" onclick={() => void clearFilters()}>{$t`Clear`}</Button>
          </div>
        {:else if filtering}
          <section aria-label={$t`Results`}>
            <div class="grid gap-3 sm:grid-cols-2">
              {#each flatResults as example (example.key)}
                {@render exampleCard(example, true)}
              {/each}
            </div>
          </section>
        {:else}
          {#each groupedView as group (group.function.key)}
            <section aria-labelledby="fn-{group.function.key}">
              <h3 id="fn-{group.function.key}" class="text-xs font-semibold uppercase tracking-wide text-muted-foreground mb-2">
                {group.function.label}
              </h3>
              <div class="grid gap-3 sm:grid-cols-2">
                {#each group.plugins as example (example.key)}
                  {@render exampleCard(example, false)}
                {/each}
              </div>
            </section>
          {/each}
        {/if}
      </div>
    {:else}
      <Dialog.Header>
        <Dialog.Title>
          {#if plugin}
            {$t`Edit plugin`}
          {:else}
            {$t`New plugin`}
          {/if}
        </Dialog.Title>
        <Dialog.Description>
          {$t`A plugin is a single HTML file. Ask an AI to write one for you (see “Get AI prompt”), start from an example, or write it yourself.`}
        </Dialog.Description>
      </Dialog.Header>

      <div class="space-y-4">
        <Alert>
          <Icon icon="i-mdi-shield-alert-outline" class="text-amber-500" />
          <AlertDescription>
            {$t`Plugins are code and run for everyone on this project. Only add plugins you or your team created, or that come from someone you trust.`}
          </AlertDescription>
        </Alert>

        {#if loadError}
          <Alert variant="destructive">
            <Icon icon="i-mdi-alert-circle-outline" />
            <AlertDescription>{$t`Couldn’t load that example. Please try again.`}</AlertDescription>
          </Alert>
        {/if}

        {#if !plugin}
          <div class="flex flex-wrap items-center gap-2">
            <Button variant="outline" size="sm" icon="i-mdi-view-grid-plus-outline" onclick={() => void openGallery()}>
              {$t`Start from an example`}
            </Button>
            <Button variant="outline" size="sm" icon="i-mdi-upload" onclick={() => fileInput?.click()}>
              {$t`Import from file`}
            </Button>
            <input bind:this={fileInput} type="file" accept=".html,.htm" class="hidden" onchange={importFile} />
          </div>
        {/if}

        <div class="space-y-2">
          <Label for="plugin-name">{$t`Name`}</Label>
          <Input id="plugin-name" bind:value={name} oninput={() => nameAutoFilled = false} placeholder={$t`e.g. Dictionary stats`} />
        </div>

        <div class="space-y-2">
          <Label for="plugin-description">{$t`Description`}</Label>
          <Input
            id="plugin-description"
            bind:value={description}
            oninput={() => descriptionAutoFilled = false}
            placeholder={$t`e.g. Dashboard of dictionary statistics`}
          />
          <p class="text-xs text-muted-foreground">{$t`A short summary shown on the plugin’s card.`}</p>
        </div>

        <div class="space-y-2">
          <div class="flex items-center justify-between">
            <Label for="plugin-html">{$t`Plugin HTML`}</Label>
            <div class="flex items-center gap-2 text-xs text-muted-foreground">
              {#if permissions.includes('internet')}
                <Badge variant="destructive">
                  <Icon icon="i-mdi-web" />
                  {$t`Requests internet access`}
                </Badge>
              {/if}
              {#if html.trim()}
                <span>{$t`${sizeKb} KB`}</span>
              {/if}
            </div>
          </div>
          <Textarea
            id="plugin-html"
            bind:value={html}
            placeholder={$t`Paste the plugin HTML here`}
            spellcheck="false"
            class="font-mono text-xs min-h-64 max-h-[50vh]"
          />
        </div>
      </div>

      <Dialog.Footer>
        <Button variant="outline" onclick={() => open = false}>{$t`Cancel`}</Button>
        <Button disabled={!canSave} loading={saving} onclick={() => void save()}>
          {#if plugin}
            {$t`Save changes`}
          {:else}
            {$t`Add plugin`}
          {/if}
        </Button>
      </Dialog.Footer>
    {/if}
  </Dialog.Content>
</Dialog.Root>
