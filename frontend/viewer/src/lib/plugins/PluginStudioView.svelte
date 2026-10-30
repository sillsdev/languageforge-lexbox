<script lang="ts">
  import * as AlertDialog from '$lib/components/ui/alert-dialog';
  import * as Collapsible from '$lib/components/ui/collapsible';
  import * as Tabs from '$lib/components/ui/tabs';
  import {Badge} from '$lib/components/ui/badge';
  import {Button} from '$lib/components/ui/button';
  import {Icon} from '$lib/components/ui/icon';
  import {Input} from '$lib/components/ui/input';
  import {Label} from '$lib/components/ui/label';
  import {Switch} from '$lib/components/ui/switch';
  import {Textarea} from '$lib/components/ui/textarea';
  import {t} from 'svelte-i18n-lingui';
  import {onMount, untrack} from 'svelte';
  import {navigate, useRouter} from 'svelte-routing';
  import {type IEntry, type IPlugin, SortField, UserProjectRole} from '$lib/dotnet-types';
  import {usePluginService} from '$project/data/plugin-service.svelte';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useWritingSystemService} from '$project/data';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {setLeaveGuard} from '$lib/utils/leave-guard';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import EntryOrSensePicker from '$lib/entry-editor/EntryOrSensePicker.svelte';
  import {examplePlugins, type ExampleCapability} from './examples';
  import {extractPluginHtml, isCompletePluginHtml, pluginHtmlTitle} from './plugin-html';
  import {createPluginDocUrl} from './plugin-srcdoc';
  import {buildPluginPrompt, pluginTaskSection, type PluginPromptOptions} from './plugin-prompt';
  import {PluginStudioDraft, type StudioMode} from './plugin-studio-draft.svelte';
  import PluginSandbox from './PluginSandbox.svelte';
  import type {IconClass} from '$lib/icon-class';

  interface Props {
    mode: StudioMode;
    /** Edit mode: the plugin being edited. */
    pluginId?: string;
    /** Example mode: the example being tried. */
    exampleKey?: string;
  }

  const {mode, pluginId, exampleKey}: Props = $props();

  const pluginService = usePluginService();
  const projectContext = useProjectContext();
  const writingSystemService = useWritingSystemService();
  const dialogsService = useDialogsService();
  const {base} = useRouter();

  // Local-only projects have no role info; there the user owns their data anyway.
  const canManage = $derived(!projectContext.projectData || projectContext.projectData.role === UserProjectRole.Manager);

  // The route keys this view, so the mode and the plugin/example it is about never change.
  const draft = untrack(() => new PluginStudioDraft(mode));
  const example = untrack(() => exampleKey ? examplePlugins.find(candidate => candidate.key === exampleKey) : undefined);
  const plugin = $derived(pluginId ? pluginService.current.find(candidate => candidate.id === pluginId) : undefined);

  type LoadState = 'loading' | 'ready' | 'missing' | 'offline' | 'error';
  let loadState = $state<LoadState>(untrack(() => mode) === 'new' ? 'ready' : 'loading');
  /** The saved plugin the current baseline came from (edit mode), so a save updates the right version. */
  let savedPlugin = $state.raw<IPlugin>();
  /** Set once an example or new plugin has been added to the project. */
  let added = $state.raw<IPlugin>();

  onMount(() => {
    if (mode !== 'example') return;
    if (!example) {
      loadState = 'missing';
      return;
    }
    void example.loadHtml().then(html => {
      draft.reset({name: example.name, description: example.description, html});
      loadState = 'ready';
    });
  });

  $effect(() => {
    if (mode !== 'edit' || savedPlugin) return;
    if (!plugin) {
      if (pluginService.loaded) loadState = 'missing';
      return;
    }
    const target = plugin;
    savedPlugin = target;
    void pluginService.getHtml(target).then(result => {
      if (result.result === 'ok') {
        draft.reset({name: target.name, description: target.description ?? '', html: result.html});
        loadState = 'ready';
      } else {
        loadState = result.result;
      }
    });
  });

  // ---- Preview ----

  let reloadToken = $state(0);
  let phoneWidth = $state(false);
  const previewDoc = $derived.by(() => {
    if (!draft.manifest) return undefined;
    return {html: draft.html, manifest: draft.manifest};
  });
  let docUrl = $state<string>();
  $effect(() => {
    const doc = previewDoc;
    if (!doc) {
      docUrl = undefined;
      return;
    }
    const url = createPluginDocUrl(doc.html, doc.manifest.permissions);
    docUrl = url;
    return () => URL.revokeObjectURL(url);
  });

  // Entry-menu plugins expect an entry to start from; preview them with a real one.
  const needsEntry = $derived(!!previewDoc?.manifest.contexts.includes('entry'));
  let previewEntry = $state.raw<IEntry>();
  $effect(() => {
    if (!needsEntry || previewEntry) return;
    void projectContext.api.getEntries({
      count: 1,
      offset: 0,
      order: {field: SortField.Headword, writingSystem: 'default', ascending: true},
    }).then(entries => {
      previewEntry ??= entries[0];
    });
  });
  const previewEntryLabel = $derived(previewEntry ? writingSystemService.firstHeadword(previewEntry) || $t`(no headword)` : $t`Pick an entry`);

  // ---- Tabs ----

  type Tab = 'about' | 'ai' | 'source' | 'details';
  const tabs: Tab[] = untrack(() => mode === 'example' ? ['about', 'ai', 'source'] : mode === 'new' ? ['ai', 'source', 'details'] : ['ai', 'source', 'details']);
  let tab = $state<Tab>(tabs[0]);
  const tabLabels = $derived<Record<Tab, string>>({
    about: $t`About`,
    ai: $t`Ask AI`,
    source: mode === 'new' ? $t`Paste or upload` : $t`Source`,
    details: $t`Details`,
  });

  /** Phones show one pane at a time. */
  let mobilePane = $state<'preview' | 'edit'>('preview');

  // ---- Ask AI (a new plugin) ----

  let aiDescription = $state('');
  let basePrompt = $state('');
  let copied = $state(false);
  let showPromptOptions = $state(false);
  // These only steer the generated prompt; they don't restrict what a finished plugin can do.
  const promptOptions = $state<PluginPromptOptions>({
    projectSpecific: true,
    mobile: true,
    allowEdits: true,
    internet: false,
    culturalSensitivity: false,
  });
  let promptGeneration = 0;
  $effect(() => {
    if (mode !== 'new') return;
    const current = {...promptOptions};
    const token = ++promptGeneration;
    void buildPluginPrompt(projectContext.api, {
      projectName: projectContext.projectName,
      projectCode: projectContext.projectCode,
    }, current).then(result => { if (token === promptGeneration) basePrompt = result; });
  });
  const newPluginPrompt = $derived(basePrompt ? `${basePrompt}\n\n${pluginTaskSection(aiDescription)}` : '');
  $effect(() => {
    newPluginPrompt; // eslint-disable-line @typescript-eslint/no-unused-expressions
    copied = false;
  });

  async function copyPrompt(prompt: string) {
    await navigator.clipboard.writeText(prompt);
    copied = true;
    AppNotification.display($t`Prompt copied. Paste it into any AI chat.`, {timeout: 'short', type: 'success'});
  }

  // ---- Paste / upload ----

  let pasteText = $state('');
  let pasteError = $state<string>();
  let fileInput = $state<HTMLInputElement>();
  // A name taken from the plugin's <title> keeps following new versions until the user types one.
  let nameFromHtml = $state(false);

  function useHtml(candidate: string): boolean {
    const html = extractPluginHtml(candidate);
    if (!isCompletePluginHtml(html)) {
      pasteError = $t`This doesn't look like a complete plugin. It should be one HTML file that starts with <!DOCTYPE html> and ends with </html>.`;
      return false;
    }
    pasteError = undefined;
    draft.html = html;
    if (mode === 'new' && (!draft.name.trim() || nameFromHtml)) {
      const title = pluginHtmlTitle(html);
      if (title) {
        draft.name = title;
        nameFromHtml = true;
      }
    }
    mobilePane = 'preview';
    return true;
  }

  function applyPaste() {
    if (useHtml(pasteText)) pasteText = '';
  }

  async function uploadFile(event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    useHtml(await file.text());
  }

  function downloadHtml() {
    const url = URL.createObjectURL(new Blob([draft.html], {type: 'text/html'}));
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = `${draft.name.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '') || 'plugin'}.html`;
    anchor.click();
    URL.revokeObjectURL(url);
  }

  // ---- Saving ----

  let saving = $state(false);
  function draftToSave() {
    return {name: draft.name.trim(), description: draft.description.trim() || undefined, html: draft.html};
  }

  async function addToProject() {
    saving = true;
    try {
      added = mode === 'example' && example
        ? await pluginService.addExample(example, draftToSave(), draft.customized)
        : await pluginService.create(draftToSave());
      AppNotification.display($t`"${added.name}" was added for everyone on this project.`, {timeout: 'short', type: 'success'});
    } finally {
      saving = false;
    }
  }

  async function saveEdit() {
    if (!savedPlugin) return;
    saving = true;
    try {
      const saved = draftToSave();
      const updated = await pluginService.update(savedPlugin, saved, draft.baseline.html);
      savedPlugin = updated;
      draft.reset({name: updated.name, description: updated.description ?? '', html: saved.html});
      AppNotification.display($t`Saved for the team.`, {timeout: 'short', type: 'success'});
    } finally {
      saving = false;
    }
  }

  async function removePlugin() {
    if (!savedPlugin) return;
    const shouldDelete = await dialogsService.promptDelete($t`Plugin`, savedPlugin.name, {
      isDangerous: true,
      details: $t`This removes the plugin for everyone on this project.`,
    });
    if (!shouldDelete) return;
    await pluginService.delete(savedPlugin.id);
    added = savedPlugin; // nothing left to lose
    goToPlugins();
  }

  // ---- Leaving ----

  const hasUnsavedWork = $derived(!added && draft.dirty);
  let leavePrompt = $state<(leave: boolean) => void>();
  function confirmLeave(): Promise<boolean> {
    if (!hasUnsavedWork) return Promise.resolve(true);
    return new Promise(resolve => leavePrompt = resolve);
  }
  function answerLeavePrompt(leave: boolean) {
    leavePrompt?.(leave);
    leavePrompt = undefined;
  }

  $effect(() => setLeaveGuard(confirmLeave));

  // Browser/Android back while there's unsaved work asks first; staying re-arms the guard.
  let backGuardVersion = $state(0);
  useBackHandler({
    addToStack: () => {
      backGuardVersion; // eslint-disable-line @typescript-eslint/no-unused-expressions
      return hasUnsavedWork;
    },
    onBack: () => void confirmLeave().then(leave => {
      if (leave) goToPlugins();
      else backGuardVersion++;
    }),
    key: 'plugin-studio',
  });

  function onBeforeUnload(event: BeforeUnloadEvent) {
    if (hasUnsavedWork) event.preventDefault();
  }

  function goToPlugins() {
    navigate(`${$base.uri}/plugins`);
  }

  async function leave() {
    if (await confirmLeave()) goToPlugins();
  }

  function discardChanges() {
    draft.reset(draft.baseline);
    nameFromHtml = false;
  }

  // ---- Display ----

  const title = $derived(draft.name.trim() || (mode === 'new' ? $t`New plugin` : example?.name ?? plugin?.name ?? $t`Plugin`));

  const statusChip = $derived.by((): {label: string; tone: 'muted' | 'ok' | 'warn'} | undefined => {
    if (added) return {label: $t`In this project`, tone: 'ok'};
    if (mode === 'example') return draft.customized
      ? {label: $t`Your version, not added yet`, tone: 'muted'}
      : {label: $t`Example, not in this project`, tone: 'muted'};
    if (mode === 'new') return {label: $t`Draft, only you can see it`, tone: 'muted'};
    return draft.dirty ? {label: $t`Unsaved changes`, tone: 'warn'} : undefined;
  });

  const capabilityLabels: Record<ExampleCapability, string> = $derived({
    internet: $t`Uses the internet`,
    microphone: $t`Records from the microphone`,
    camera: $t`Uses the camera`,
    'entry-menu': $t`Runs from an entry's menu`,
  });

  const asks = $derived.by(() => {
    const manifest = draft.manifest;
    if (!manifest) return [];
    const lines = [
      manifest.permissions.includes('edit')
        ? $t`Can change the dictionary, and asks before each change`
        : $t`Reads the dictionary, never changes it`,
      manifest.permissions.includes('internet') ? $t`Uses the internet` : $t`No internet`,
    ];
    if (manifest.contexts.includes('entry')) lines.push(capabilityLabels['entry-menu']);
    for (const capability of example?.capabilities ?? []) {
      if (capability === 'microphone' || capability === 'camera') lines.push(capabilityLabels[capability]);
    }
    return lines;
  });

  const sizeKb = $derived(Math.max(1, Math.round(draft.html.length / 1024)));
</script>

<svelte:window onbeforeunload={onBeforeUnload} />

{#snippet pasteBox(label: string)}
  <div class="space-y-2">
    <Label for="plugin-studio-paste">{label}</Label>
    <Textarea
      id="plugin-studio-paste"
      bind:value={pasteText}
      spellcheck="false"
      placeholder={$t`Paste the AI's reply or the plugin's HTML here`}
      class="font-mono text-xs min-h-28 max-h-[40vh]"
    />
    {#if pasteError}
      <p class="text-sm text-destructive">{pasteError}</p>
    {/if}
    <Button variant="outline" size="sm" icon="i-mdi-check" disabled={!pasteText.trim()} onclick={applyPaste}>
      {$t`Use this version`}
    </Button>
  </div>
{/snippet}

{#snippet newPluginAi()}
  <ol class="space-y-5">
    <li class="space-y-2">
      <div class="font-medium">{$t`1. Describe your plugin`}</div>
      <Textarea
        bind:value={aiDescription}
        aria-label={$t`Describe your plugin`}
        class="min-h-24"
        placeholder={$t`e.g. A flashcard game that quizzes me on entries missing an English gloss and lets me fill it in when I remember the word.`}
      />
      <Collapsible.Root bind:open={showPromptOptions}>
        <Collapsible.Trigger class="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
          <Icon icon="i-mdi-chevron-right" class="transition-transform {showPromptOptions ? 'rotate-90' : ''}" />
          {$t`Prompt options`}
        </Collapsible.Trigger>
        <Collapsible.Content class="space-y-3 pt-2 text-sm">
          <p class="text-xs text-muted-foreground">
            {$t`These guide how the AI writes the plugin. They don't restrict what the finished plugin can do.`}
          </p>
          <Switch label={$t`Tailor to this project`} bind:checked={promptOptions.projectSpecific} />
          <Switch label={$t`Works on phones`} bind:checked={promptOptions.mobile} />
          <Switch label={$t`Can change the dictionary`} bind:checked={promptOptions.allowEdits} />
          <Switch label={$t`Can use the internet`} bind:checked={promptOptions.internet} />
          <Switch label={$t`Handle content with cultural care`} bind:checked={promptOptions.culturalSensitivity} />
        </Collapsible.Content>
      </Collapsible.Root>
    </li>
    <li class="space-y-2">
      <div class="font-medium">{$t`2. Have an AI write it`}</div>
      <p class="text-sm text-muted-foreground">
        {$t`Copy the prompt into any AI chat (e.g. Claude or ChatGPT). It already explains plugins and this project.`}
      </p>
      <Button
        icon={copied ? 'i-mdi-check' : 'i-mdi-content-copy'}
        disabled={!newPluginPrompt}
        loading={!newPluginPrompt}
        onclick={() => void copyPrompt(newPluginPrompt)}
      >
        {copied ? $t`Copied` : $t`Copy prompt`}
      </Button>
    </li>
    <li class="space-y-2">
      <div class="font-medium">{$t`3. Try it here`}</div>
      {@render pasteBox($t`Paste the AI's reply`)}
    </li>
  </ol>
{/snippet}

{#snippet panelContent(current: Tab)}
  {#if current === 'about' && example}
    <div class="space-y-4">
      <p>{example.description}</p>
      <div class="space-y-2">
        <h3 class="text-sm font-semibold">{$t`What it asks for`}</h3>
        <ul class="space-y-1.5 text-sm">
          {#each asks as line (line)}
            <li class="rounded-md bg-muted px-3 py-2">{line}</li>
          {/each}
        </ul>
      </div>
      <div class="space-y-1">
        <h3 class="text-sm font-semibold">{$t`If you add it`}</h3>
        <p class="text-sm text-muted-foreground">
          {$t`Everyone on this project will see it under Plugins. You can change or remove it later.`}
        </p>
      </div>
      <Button variant="outline" icon="i-mdi-creation" onclick={() => tab = 'ai'}>{$t`Make it your own with AI`}</Button>
    </div>
  {:else if current === 'ai'}
    {#if mode === 'new' && !draft.html}
      {@render newPluginAi()}
    {:else}
      {@render pasteBox($t`Paste a new version`)}
    {/if}
  {:else if current === 'source'}
    <div class="flex h-full flex-col gap-3">
      <div class="flex flex-wrap gap-2">
        <Button variant="outline" size="sm" icon="i-mdi-upload" onclick={() => fileInput?.click()}>{$t`Upload file`}</Button>
        {#if draft.html}
          <Button variant="outline" size="sm" icon="i-mdi-download" onclick={downloadHtml}>{$t`Download`}</Button>
          <span class="self-center text-xs text-muted-foreground">{$t`${sizeKb} KB`}</span>
        {/if}
        <input bind:this={fileInput} type="file" accept=".html,.htm" class="hidden" onchange={uploadFile} />
      </div>
      {#if mode === 'new' && !draft.html}
        <p class="text-sm text-muted-foreground">{$t`Already have a plugin file? It runs in the preview straight away, before anyone else can see it.`}</p>
        {@render pasteBox($t`Paste HTML`)}
      {:else}
        <Textarea
          readonly
          value={draft.html}
          aria-label={$t`Plugin HTML`}
          spellcheck="false"
          class="font-mono text-xs grow min-h-64"
        />
      {/if}
    </div>
  {:else if current === 'details'}
    <div class="space-y-4">
      <div class="space-y-2">
        <Label for="plugin-studio-name">{$t`Name`}</Label>
        <Input id="plugin-studio-name" bind:value={draft.name} oninput={() => nameFromHtml = false} placeholder={$t`e.g. Dictionary stats`} />
        {#if nameFromHtml}
          <p class="text-xs text-muted-foreground">{$t`Filled in from the plugin's title. Change it if you like.`}</p>
        {/if}
      </div>
      <div class="space-y-2">
        <Label for="plugin-studio-description">{$t`Description`}</Label>
        <Input id="plugin-studio-description" bind:value={draft.description} placeholder={$t`e.g. Dashboard of dictionary statistics`} />
        <p class="text-xs text-muted-foreground">{$t`A short summary shown on the plugin's card.`}</p>
      </div>
      {#if mode === 'edit'}
        <Button variant="outline" class="text-destructive" icon="i-mdi-trash-can-outline" onclick={() => void removePlugin()}>
          {$t`Remove from project`}
        </Button>
      {/if}
    </div>
  {/if}
{/snippet}

{#snippet headerActions()}
  {#if added}
    <span class="text-sm font-medium text-green-700 dark:text-green-400">{$t`Added for the team`}</span>
    <Button variant="outline" icon="i-mdi-play" onclick={() => navigate(`${$base.uri}/plugins/${added!.id}`)}>{$t`Run it`}</Button>
    <Button onclick={goToPlugins}>{$t`Back to plugins`}</Button>
  {:else if mode === 'example'}
    <Button variant="outline" onclick={() => void leave()}>{$t`Close`}</Button>
    <Button icon="i-mdi-plus" disabled={!draft.canSave} loading={saving} onclick={() => void addToProject()}>
      {draft.customized ? $t`Add my version` : $t`Add to project`}
    </Button>
  {:else if mode === 'new'}
    <Button variant="outline" onclick={() => void leave()}>{$t`Discard`}</Button>
    <Button icon="i-mdi-plus" disabled={!draft.canSave} loading={saving} onclick={() => void addToProject()}>
      {$t`Add to project`}
    </Button>
  {:else}
    <Button variant="outline" disabled={!draft.dirty} onclick={discardChanges}>{$t`Discard`}</Button>
    <Button icon="i-mdi-content-save" disabled={!draft.canSave} loading={saving} onclick={() => void saveEdit()}>
      {$t`Save for the team`}
    </Button>
  {/if}
{/snippet}

{#snippet notice(icon: IconClass, message: string)}
  <div class="h-full grid place-items-center p-4">
    <div class="text-center space-y-3 max-w-md">
      <Icon {icon} class="size-10 text-muted-foreground" />
      <p>{message}</p>
      <Button variant="outline" onclick={goToPlugins}>{$t`Back to plugins`}</Button>
    </div>
  </div>
{/snippet}

{#if !canManage}
  {@render notice('i-mdi-lock-outline', $t`Only project managers can add or change plugins.`)}
{:else if loadState === 'missing'}
  {@render notice('i-mdi-puzzle-remove', mode === 'example' ? $t`This example doesn't exist.` : $t`This plugin no longer exists.`)}
{:else if loadState === 'offline'}
  {@render notice('i-mdi-cloud-off-outline', $t`You're offline and this plugin's file isn't on this device yet. Connect and try again.`)}
{:else if loadState === 'error'}
  {@render notice('i-mdi-alert-circle-outline', $t`Couldn't load the plugin file.`)}
{:else}
  <div class="h-full flex flex-col bg-background" data-testid="plugin-studio">
    <header class="flex flex-wrap items-center gap-x-3 gap-y-2 border-b px-4 py-2.5">
      <nav aria-label={$t`Breadcrumb`} class="flex min-w-0 items-center gap-2">
        <Button variant="link" class="px-0" onclick={() => void leave()}>{$t`Plugins`}</Button>
        <span class="text-muted-foreground" aria-hidden="true">/</span>
        {#if mode === 'example'}
          <span class="text-sm text-muted-foreground max-sm:hidden">{$t`Examples`}</span>
          <span class="text-muted-foreground max-sm:hidden" aria-hidden="true">/</span>
        {/if}
        <h1 class="truncate font-semibold">{title}</h1>
      </nav>
      {#if statusChip}
        <span
          class="rounded-full px-2.5 py-0.5 text-xs font-semibold"
          class:bg-muted={statusChip.tone === 'muted'}
          class:text-muted-foreground={statusChip.tone === 'muted'}
          class:bg-green-100={statusChip.tone === 'ok'}
          class:text-green-800={statusChip.tone === 'ok'}
          class:bg-amber-100={statusChip.tone === 'warn'}
          class:text-amber-800={statusChip.tone === 'warn'}
        >
          {statusChip.label}
        </span>
      {/if}
      <div class="grow"></div>
      <div class="flex flex-wrap items-center gap-2">
        {@render headerActions()}
      </div>
    </header>

    {#if IsMobile.value}
      <Tabs.Root bind:value={mobilePane} class="px-3 pt-2">
        <Tabs.List class="w-full">
          <Tabs.Trigger value="preview" class="flex-1">{$t`Preview`}</Tabs.Trigger>
          <Tabs.Trigger value="edit" class="flex-1">{$t`Edit`}</Tabs.Trigger>
        </Tabs.List>
      </Tabs.Root>
    {/if}

    <div class="flex min-h-0 grow">
      <section
        aria-label={$t`Preview`}
        class="flex min-w-0 grow flex-col bg-muted/50"
        class:hidden={IsMobile.value && mobilePane !== 'preview'}
      >
        <div class="flex flex-wrap items-center gap-2 px-4 py-2 text-sm text-muted-foreground">
          <Badge variant="secondary" class="text-primary">
            <Icon icon="i-mdi-eye-outline" />
            {$t`Preview`}
          </Badge>
          <span>{$t`Your real dictionary. Changes it tries are shown, not saved.`}</span>
          <div class="grow"></div>
          {#if needsEntry}
            <EntryOrSensePicker title={$t`Preview with entry`} mode="only-entries" pick={(selection) => previewEntry = selection.entry}>
              {#snippet trigger({props})}
                <Button {...props} variant="outline" size="sm" icon="i-mdi-book-open-variant" title={$t`Entry the plugin starts from`}>
                  <span class="max-w-40 truncate">{previewEntryLabel}</span>
                </Button>
              {/snippet}
            </EntryOrSensePicker>
          {/if}
          {#if !IsMobile.value}
            <Button
              variant="outline"
              size="sm"
              icon="i-mdi-cellphone"
              aria-pressed={phoneWidth}
              onclick={() => phoneWidth = !phoneWidth}
            >
              {$t`Phone width`}
            </Button>
          {/if}
          <Button variant="outline" size="sm" icon="i-mdi-refresh" disabled={!docUrl} onclick={() => reloadToken++}>{$t`Reload`}</Button>
        </div>
        <div class="mx-4 mb-4 flex min-h-0 grow justify-center">
          <div
            class="relative h-full w-full overflow-hidden rounded-lg border bg-background"
            class:max-w-[390px]={phoneWidth}
            data-testid="plugin-studio-preview"
          >
            {#if loadState === 'loading'}
              <div class="absolute inset-0 grid place-items-center text-muted-foreground">
                <Icon icon="i-mdi-loading" class="animate-spin size-8" />
              </div>
            {:else if docUrl && previewDoc && (!needsEntry || previewEntry)}
              {#key `${docUrl}|${previewEntry?.id ?? ''}`}
                <PluginSandbox
                  pluginId="studio-preview"
                  pluginName={title}
                  {docUrl}
                  manifest={previewDoc.manifest}
                  writeMode="preview"
                  entryId={needsEntry ? previewEntry?.id : undefined}
                  {reloadToken}
                />
              {/key}
            {:else if needsEntry}
              <div class="absolute inset-0 grid place-items-center p-6 text-center text-muted-foreground">
                {$t`This plugin runs from an entry's menu. Pick an entry to preview it with.`}
              </div>
            {:else}
              <div class="absolute inset-0 grid place-items-center p-6">
                <div class="max-w-sm space-y-2 text-center">
                  <Icon icon="i-mdi-puzzle-outline" class="size-10 text-muted-foreground" />
                  <div class="font-medium">{$t`Your plugin will run here`}</div>
                  <p class="text-sm text-muted-foreground">
                    {$t`Describe it and have an AI write it, or paste a plugin file you already have.`}
                  </p>
                </div>
              </div>
            {/if}
          </div>
        </div>
      </section>

      <aside
        aria-label={$t`Plugin`}
        class="flex w-full shrink-0 flex-col border-s md:w-[440px]"
        class:hidden={IsMobile.value && mobilePane !== 'edit'}
      >
        <Tabs.Root bind:value={tab} class="min-h-0 grow gap-0">
          <Tabs.List class="m-3 mb-0 w-auto">
            {#each tabs as candidate (candidate)}
              <Tabs.Trigger value={candidate} class="flex-1">{tabLabels[candidate]}</Tabs.Trigger>
            {/each}
          </Tabs.List>
          {#each tabs as candidate (candidate)}
            <Tabs.Content value={candidate} class="min-h-0 grow overflow-y-auto p-4">
              {#if loadState === 'ready'}
                {@render panelContent(candidate)}
              {/if}
            </Tabs.Content>
          {/each}
        </Tabs.Root>
      </aside>
    </div>
  </div>
{/if}

<AlertDialog.Root open={!!leavePrompt} onOpenChange={(open) => { if (!open) answerLeavePrompt(false); }}>
  <AlertDialog.Content>
    <AlertDialog.Header>
      <AlertDialog.Title>{$t`Leave without saving?`}</AlertDialog.Title>
      <AlertDialog.Description>
        {#if mode === 'edit'}
          {$t`Your changes to this plugin will be lost.`}
        {:else}
          {$t`This plugin hasn't been added to the project, so it will be lost.`}
        {/if}
      </AlertDialog.Description>
    </AlertDialog.Header>
    <AlertDialog.Footer>
      <AlertDialog.Cancel onclick={() => answerLeavePrompt(false)}>{$t`Keep editing`}</AlertDialog.Cancel>
      <AlertDialog.Action onclick={() => answerLeavePrompt(true)}>{$t`Leave`}</AlertDialog.Action>
    </AlertDialog.Footer>
  </AlertDialog.Content>
</AlertDialog.Root>
