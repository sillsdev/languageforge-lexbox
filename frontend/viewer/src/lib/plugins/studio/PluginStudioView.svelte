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
  import {onDestroy, untrack} from 'svelte';
  import {navigate, useRouter} from 'svelte-routing';
  import {type IEntry, SortField, UserProjectRole} from '$lib/dotnet-types';
  import {usePluginService} from '$project/data/plugin-service.svelte';
  import {useProjectContext} from '$project/project-context.svelte';
  import {useWritingSystemService} from '$project/data';
  import {useDialogsService} from '$lib/services/dialogs-service';
  import {AppNotification} from '$lib/notifications/notifications';
  import {useBackHandler} from '$lib/utils/back-handler.svelte';
  import {setLeaveGuard} from '$lib/utils/leave-guard';
  import {IsMobile} from '$lib/hooks/is-mobile.svelte';
  import EntryOrSensePicker from '$lib/entry-editor/EntryOrSensePicker.svelte';
  import type {ExampleCapability} from '../examples';
  import {createPluginDocUrl} from '../plugin-srcdoc';
  import {buildPluginPrompt, pluginTaskSection, type PluginPromptOptions} from '../plugin-prompt';
  import type {PluginPermission} from '../plugin-api-types';
  import type {StudioMode} from './plugin-studio-draft.svelte';
  import {PluginStudio, setStudio, type StudioStatus, type StudioTab} from './plugin-studio.svelte';
  import {downloadPluginHtml, downloadPrompt} from './studio-files';
  import PluginSandbox from '../PluginSandbox.svelte';
  import PluginConsentCard from '../PluginConsentCard.svelte';
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
  const studio = untrack(() => new PluginStudio({
    mode,
    pluginId,
    exampleKey,
    pluginService,
    project: projectContext,
    onGone: goToPlugins,
  }));
  setStudio(studio);
  onDestroy(() => studio.destroy());
  const draft = studio.draft;
  const example = studio.example;

  // ---- Preview ----

  let reloadToken = $state(0);
  let phoneWidth = $state(false);
  let docUrl = $state<string>();
  $effect(() => {
    const doc = studio.previewDoc;
    if (!doc) {
      docUrl = undefined;
      return;
    }
    const url = createPluginDocUrl(doc.html, doc.manifest.permissions);
    docUrl = url;
    return () => URL.revokeObjectURL(url);
  });

  // Entry-menu plugins expect an entry to start from; preview them with a real one.
  const needsEntry = $derived(!!studio.previewDoc?.manifest.contexts.includes('entry'));
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

  const tabLabels = $derived<Record<StudioTab, string>>({
    about: $t`About`,
    ai: $t`Ask AI`,
    source: mode === 'new' ? $t`Paste or upload` : $t`Source`,
    details: $t`Details`,
  });

  // ---- Ask AI (a new plugin) ----

  let aiDescription = $state('');
  let basePrompt = $state('');
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

  // ---- Ask AI (changing a plugin) ----

  let changeText = $state('');

  // ---- Reviewing a new version ----

  const otherChanges = $derived.by(() => {
    if (!studio.review) return [];
    const {added, removed} = studio.review.changes;
    return [
      ...added.permissions.filter(permission => studio.keptPermissions.includes(permission))
        .map(permission => permission === 'internet' ? $t`Now uses the internet` : $t`Now can change the dictionary`),
      ...removed.permissions.map(permission => permission === 'internet' ? $t`No longer uses the internet` : $t`No longer changes the dictionary`),
      ...added.contexts.map(() => $t`Now runs from an entry's menu`),
      ...removed.contexts.map(() => $t`No longer runs from an entry's menu`),
      ...added.requires.map(feature => $t`Now needs ${feature}, which not every project has`),
      ...removed.requires.map(feature => $t`No longer needs ${feature}`),
    ];
  });
  const permissionWarnings: Record<PluginPermission, string> = $derived({
    internet: $t`The new version asks to use the internet`,
    edit: $t`The new version asks to change the dictionary`,
  });

  // ---- Paste / upload ----

  let fileInput = $state<HTMLInputElement>();

  async function uploadFile(event: Event) {
    const input = event.currentTarget as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    await studio.useHtml(await file.text());
  }

  // ---- Saving ----

  async function addToProject() {
    const added = await studio.addToProject();
    AppNotification.display($t`"${added.name}" was added for everyone on this project.`, {timeout: 'short', type: 'success'});
  }

  async function saveEdit() {
    await studio.saveEdit();
    AppNotification.display($t`Saved for the team.`, {timeout: 'short', type: 'success'});
  }

  async function removePlugin() {
    if (!studio.savedPlugin) return;
    const shouldDelete = await dialogsService.promptDelete($t`Plugin`, studio.savedPlugin.name, {
      isDangerous: true,
      details: $t`This removes the plugin for everyone on this project.`,
    });
    if (shouldDelete) await studio.removePlugin();
  }

  // ---- Leaving ----

  $effect(() => setLeaveGuard(() => studio.confirmLeave()));

  // Browser/Android back while there's unsaved work asks first; staying re-arms the guard.
  let backGuardVersion = $state(0);
  useBackHandler({
    addToStack: () => {
      backGuardVersion; // eslint-disable-line @typescript-eslint/no-unused-expressions
      return studio.hasUnsavedWork;
    },
    onBack: () => void studio.confirmLeave().then(leave => {
      if (leave) goToPlugins();
      else backGuardVersion++;
    }),
    key: 'plugin-studio',
  });

  function onBeforeUnload(event: BeforeUnloadEvent) {
    if (studio.hasUnsavedWork) event.preventDefault();
  }

  function goToPlugins() {
    navigate(`${$base.uri}/plugins`);
  }

  async function leave() {
    if (await studio.confirmLeave()) goToPlugins();
  }

  // ---- Display ----

  const title = $derived(studio.name ?? (mode === 'new' ? $t`New plugin` : $t`Plugin`));

  const statusChips = $derived<Record<StudioStatus, {label: string; tone: 'muted' | 'ok' | 'warn'}>>({
    added: {label: $t`In this project`, tone: 'ok'},
    example: {label: $t`Example, not in this project`, tone: 'muted'},
    'example-customized': {label: $t`Your version, not added yet`, tone: 'muted'},
    new: {label: $t`Draft, only you can see it`, tone: 'muted'},
    unsaved: {label: $t`Unsaved changes`, tone: 'warn'},
  });
  const statusChip = $derived(studio.status ? statusChips[studio.status] : undefined);

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

  const pasteMessages = $derived({
    error: $t`This doesn't look like a complete plugin. It should be one HTML file that starts with <!DOCTYPE html> and ends with </html>.`,
    note: $t`Follow-up prompt downloaded. Attach it in the same AI chat, then paste its reply here.`,
  });
</script>

<svelte:window onbeforeunload={onBeforeUnload} />

{#snippet pasteBox(label: string)}
  <div class="space-y-2">
    <Label for="plugin-studio-paste">{label}</Label>
    <Textarea
      id="plugin-studio-paste"
      bind:value={studio.pasteText}
      spellcheck="false"
      placeholder={$t`Paste the AI's reply or the plugin's HTML here`}
      class="font-mono text-xs min-h-28 max-h-[40vh]"
    />
    {#if studio.pasteError}
      <p class="text-sm text-destructive">{pasteMessages.error}</p>
    {/if}
    {#if studio.pasteNote}
      <p class="rounded-md bg-muted px-3 py-2 text-sm">{pasteMessages.note}</p>
    {/if}
    <Button variant="outline" size="sm" icon="i-mdi-check" disabled={!studio.pasteText.trim()} onclick={() => void studio.applyPaste()}>
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
        {$t`Download the prompt and attach the file in any AI chat (e.g. Claude, ChatGPT or Gemini). It already explains plugins and this project.`}
      </p>
      <Button icon="i-mdi-download" disabled={!newPluginPrompt} loading={!newPluginPrompt} onclick={() => downloadPrompt(newPluginPrompt, draft.name)}>
        {$t`Download prompt`}
      </Button>
    </li>
    <li class="space-y-2">
      <div class="font-medium">{$t`3. Try it here`}</div>
      {@render pasteBox($t`Paste the AI's reply`)}
    </li>
  </ol>
{/snippet}

{#snippet changeAi()}
  <div class="space-y-4">
    {#if studio.review}
      <div class="space-y-2" data-testid="plugin-version-review">
        <h3 class="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{$t`This version`}</h3>
        <div class="rounded-md bg-muted px-3 py-2 text-sm">
          {#if studio.review.changes.added.permissions.length === 0}
            {$t`${studio.review.sizeBefore} KB to ${studio.sizeKb} KB, no new permissions`}
          {:else}
            {$t`${studio.review.sizeBefore} KB to ${studio.sizeKb} KB`}
          {/if}
        </div>
        {#each studio.newPermissions as permission (permission)}
          <div class="space-y-2 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm" role="alert">
            <div class="font-semibold text-destructive">{permissionWarnings[permission]}</div>
            <p>{$t`The version before it didn't. If you didn't ask for this, have the AI take it out.`}</p>
            <div class="flex flex-wrap gap-2">
              <Button variant="outline" size="sm" loading={studio.buildingChangePrompt} onclick={() => void studio.askToRemove(permission)}>
                {$t`Ask the AI to remove it`}
              </Button>
              <Button variant="ghost" size="sm" onclick={() => studio.keepPermission(permission)}>{$t`Keep it`}</Button>
            </div>
          </div>
        {/each}
        {#if studio.followUpDownloaded}
          <p class="rounded-md bg-muted px-3 py-2 text-sm">
            {$t`Follow-up prompt downloaded. Attach it in the same AI chat, then paste its reply below.`}
          </p>
        {/if}
        {#if otherChanges.length}
          <ul class="list-disc space-y-1 ps-5 text-sm">
            {#each otherChanges as line (line)}
              <li>{line}</li>
            {/each}
          </ul>
        {/if}
        {#if studio.writeTrustResets}
          <p class="rounded-md bg-muted px-3 py-2 text-sm">
            {$t`Saving turns off "Always allow" on this device, so the new version asks before its first change.`}
          </p>
        {/if}
      </div>
    {/if}

    <div class="space-y-2">
      <Label for="plugin-studio-change">{mode === 'example' && !studio.added ? $t`Change it before adding` : $t`Describe a change`}</Label>
      <Textarea
        id="plugin-studio-change"
        bind:value={changeText}
        class="min-h-20"
        placeholder={$t`e.g. Also show how many entries have a recording.`}
      />
      <Button icon="i-mdi-download" disabled={!draft.complete} loading={studio.buildingChangePrompt} onclick={() => void studio.downloadChangePrompt(changeText)}>
        {$t`Download prompt`}
      </Button>
      <p class="text-xs text-muted-foreground">
        {$t`The prompt includes the API guide, this project's setup and this plugin's current code. Attach the file in any AI chat.`}
      </p>
    </div>

    {@render pasteBox($t`Paste the AI's reply`)}

    {#if draft.previousVersions.length}
      <Button variant="ghost" size="sm" icon="i-mdi-undo" onclick={() => studio.undoVersion()}>
        {$t`Go back to the previous version`}
      </Button>
    {/if}
  </div>
{/snippet}

{#snippet panelContent(current: StudioTab)}
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
      <Button variant="outline" icon="i-mdi-creation" onclick={() => studio.tab = 'ai'}>{$t`Make it your own with AI`}</Button>
    </div>
  {:else if current === 'ai'}
    {#if mode === 'new' && !draft.html}
      {@render newPluginAi()}
    {:else}
      {@render changeAi()}
    {/if}
  {:else if current === 'source'}
    <div class="flex h-full flex-col gap-3">
      <div class="flex flex-wrap gap-2">
        <Button variant="outline" size="sm" icon="i-mdi-upload" onclick={() => fileInput?.click()}>{$t`Upload file`}</Button>
        {#if draft.html}
          <Button variant="outline" size="sm" icon="i-mdi-download" onclick={() => downloadPluginHtml(draft.html, draft.name)}>{$t`Download`}</Button>
          <span class="self-center text-xs text-muted-foreground">{$t`${studio.sizeKb} KB`}</span>
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
        <Input id="plugin-studio-name" bind:value={draft.name} oninput={() => studio.nameFromHtml = false} placeholder={$t`e.g. Dictionary stats`} />
        {#if studio.nameFromHtml}
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
  {#if studio.added}
    <span class="text-sm font-medium text-green-700 dark:text-green-400">{$t`Added for the team`}</span>
    <Button variant="outline" icon="i-mdi-play" onclick={() => navigate(`${$base.uri}/plugins/${studio.added!.id}`)}>{$t`Run it`}</Button>
    <Button onclick={goToPlugins}>{$t`Back to plugins`}</Button>
  {:else if mode === 'example'}
    <Button variant="outline" onclick={() => void leave()}>{$t`Close`}</Button>
    <Button icon="i-mdi-plus" disabled={!draft.canSave} loading={studio.saving} onclick={() => void addToProject()}>
      {draft.customized ? $t`Add my version` : $t`Add to project`}
    </Button>
  {:else if mode === 'new'}
    <Button variant="outline" onclick={() => void leave()}>{$t`Discard`}</Button>
    <Button icon="i-mdi-plus" disabled={!draft.canSave} loading={studio.saving} onclick={() => void addToProject()}>
      {$t`Add to project`}
    </Button>
  {:else}
    <Button variant="outline" disabled={!draft.dirty} onclick={() => studio.discardChanges()}>{$t`Discard`}</Button>
    <Button icon="i-mdi-content-save" disabled={!draft.canSave} loading={studio.saving} onclick={() => void saveEdit()}>
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
{:else if studio.loadState === 'missing'}
  {@render notice('i-mdi-puzzle-remove', mode === 'example' ? $t`This example doesn't exist.` : $t`This plugin no longer exists.`)}
{:else if studio.loadState === 'offline'}
  {@render notice('i-mdi-cloud-off-outline', $t`You're offline and this plugin's file isn't on this device yet. Connect and try again.`)}
{:else if studio.loadState === 'error'}
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
      <Tabs.Root bind:value={studio.mobilePane} class="px-3 pt-2">
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
        class:hidden={IsMobile.value && studio.mobilePane !== 'preview'}
      >
        <div class="flex flex-wrap items-center gap-2 px-4 py-2 text-sm text-muted-foreground">
          <Badge variant="secondary" class="text-primary">
            <Icon icon="i-mdi-eye-outline" />
            {$t`Preview`}
          </Badge>
          {#if studio.canCompareVersions}
            <div role="group" aria-label={$t`Version`} class="inline-flex gap-0.5 rounded-md bg-muted p-0.5">
              <Button size="xs" variant={studio.showSavedVersion ? 'secondary' : 'ghost'} aria-pressed={studio.showSavedVersion} onclick={() => studio.showSavedVersion = true}>
                {$t`Current version`}
              </Button>
              <Button size="xs" variant={studio.showSavedVersion ? 'ghost' : 'secondary'} aria-pressed={!studio.showSavedVersion} onclick={() => studio.showSavedVersion = false}>
                {$t`New version`}
              </Button>
            </div>
          {/if}
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
            {#if studio.loadState === 'loading'}
              <div class="absolute inset-0 grid place-items-center text-muted-foreground">
                <Icon icon="i-mdi-loading" class="animate-spin size-8" />
              </div>
            {:else if studio.needsConsent && !studio.savedApproval}
              <div class="absolute inset-0 grid place-items-center text-muted-foreground">
                <Icon icon="i-mdi-loading" class="animate-spin size-8" />
              </div>
            {:else if studio.needsConsent}
              <PluginConsentCard pluginName={title} manifest={studio.previewDoc?.manifest} onApprove={() => studio.approveSaved()} onGoBack={() => void leave()} />
            {:else if docUrl && studio.previewDoc && (!needsEntry || previewEntry)}
              {#key `${docUrl}|${previewEntry?.id ?? ''}`}
                <PluginSandbox
                  pluginId="studio-preview"
                  pluginName={title}
                  {docUrl}
                  manifest={studio.previewDoc.manifest}
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
        class:hidden={IsMobile.value && studio.mobilePane !== 'edit'}
      >
        <Tabs.Root bind:value={studio.tab} class="min-h-0 grow gap-0">
          <Tabs.List class="m-3 mb-0 w-auto">
            {#each studio.tabs as candidate (candidate)}
              <Tabs.Trigger value={candidate} class="flex-1">{tabLabels[candidate]}</Tabs.Trigger>
            {/each}
          </Tabs.List>
          {#each studio.tabs as candidate (candidate)}
            <Tabs.Content value={candidate} class="min-h-0 grow overflow-y-auto p-4">
              {#if studio.loadState === 'ready'}
                {@render panelContent(candidate)}
              {/if}
            </Tabs.Content>
          {/each}
        </Tabs.Root>
      </aside>
    </div>
  </div>
{/if}

<AlertDialog.Root open={!!studio.internetPrompt} onOpenChange={(open) => { if (!open) studio.answerInternetPrompt('cancel'); }}>
  <AlertDialog.Content data-testid="plugin-internet-prompt">
    <AlertDialog.Header>
      <AlertDialog.Title>{$t`Let this version use the internet?`}</AlertDialog.Title>
      <AlertDialog.Description class="space-y-2">
        <span class="block">
          {draft.html
            ? $t`The version before it worked offline. Online, a plugin can send this project's data to any website.`
            : $t`Online, a plugin can send this project's data to any website.`}
        </span>
        <span class="block">{$t`Only allow it if the plugin needs to go online, for example to look up words or download pictures.`}</span>
      </AlertDialog.Description>
    </AlertDialog.Header>
    <AlertDialog.Footer class="gap-2">
      <AlertDialog.Cancel onclick={() => studio.answerInternetPrompt('cancel')}>{$t`Don't run it`}</AlertDialog.Cancel>
      <Button variant="outline" loading={studio.buildingChangePrompt} onclick={() => void studio.askToRemoveInternetFirst()}>
        {$t`Ask the AI to remove it`}
      </Button>
      <AlertDialog.Action onclick={() => studio.answerInternetPrompt('allow')}>{$t`Allow and run it`}</AlertDialog.Action>
    </AlertDialog.Footer>
  </AlertDialog.Content>
</AlertDialog.Root>

<AlertDialog.Root open={!!studio.leavePrompt} onOpenChange={(open) => { if (!open) studio.answerLeavePrompt(false); }}>
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
      <AlertDialog.Cancel onclick={() => studio.answerLeavePrompt(false)}>{$t`Keep editing`}</AlertDialog.Cancel>
      <AlertDialog.Action onclick={() => studio.answerLeavePrompt(true)}>{$t`Leave`}</AlertDialog.Action>
    </AlertDialog.Footer>
  </AlertDialog.Content>
</AlertDialog.Root>
