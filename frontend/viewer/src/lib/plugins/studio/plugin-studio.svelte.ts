import {createContext} from 'svelte';
import type {IMiniLcmJsInvokable, IPlugin} from '$lib/dotnet-types';
import type {PluginService} from '$project/data/plugin-service.svelte';
import {examplePlugins, type ExamplePlugin} from '../examples';
import {extractPluginHtml, isCompletePluginHtml, pluginHtmlTitle} from '../plugin-html';
import {buildPluginPrompt, pluginChangeSection, type PluginPromptOptions} from '../plugin-prompt';
import {compareManifests, parsePluginManifest} from '../plugin-manifest';
import type {PluginPermission} from '../plugin-api-types';
import {computePluginHash} from '../plugin-local-data';
import {PluginStudioDraft, type StudioMode} from './plugin-studio-draft.svelte';
import {downloadPrompt} from './studio-files';

export type StudioTab = 'about' | 'ai' | 'source' | 'details';
export type StudioLoadState = 'loading' | 'ready' | 'missing' | 'offline' | 'error';
/** Status shown next to the title; the header words it. */
export type StudioStatus = 'added' | 'example' | 'example-customized' | 'new' | 'unsaved';

export type StudioPluginService = Pick<PluginService,
  'current' | 'loaded' | 'getHtml' | 'addExample' | 'create' | 'update' | 'delete' | 'consentStore' | 'writeTrustStore'>;

export interface StudioProject {
  api: IMiniLcmJsInvokable;
  projectName: string;
  projectCode: string;
}

export interface PluginStudioOptions {
  mode: StudioMode;
  /** Edit mode: the plugin being edited. */
  pluginId?: string;
  /** Example mode: the example being tried. */
  exampleKey?: string;
  pluginService: StudioPluginService;
  project: StudioProject;
  goToPlugins: () => void;
  openPlugin: (pluginId: string) => void;
}

/**
 * Everything the studio's sections share: the draft and what it started from, what the preview
 * shows, the version review, pasting a new version (with the internet prompt), saving and
 * leaving. Text the user reads is worded by the components; this only holds the state.
 */
export class PluginStudio {
  readonly mode: StudioMode;
  readonly draft: PluginStudioDraft;
  readonly example: ExamplePlugin | undefined;
  readonly tabs: StudioTab[];
  readonly #pluginService: StudioPluginService;
  readonly #project: StudioProject;
  readonly #pluginId: string | undefined;
  readonly #destroyEffects: () => void;
  readonly goToPlugins: () => void;
  readonly openPlugin: (pluginId: string) => void;

  loadState = $state<StudioLoadState>('loading');
  /** The saved plugin the current baseline came from (edit mode), so a save updates the right version. */
  savedPlugin = $state.raw<IPlugin>();
  /** Set once an example or new plugin has been added to the project. */
  added = $state.raw<IPlugin>();
  readonly plugin = $derived.by(() => this.#pluginId
    ? this.#pluginService.current.find(candidate => candidate.id === this.#pluginId)
    : undefined);

  tab = $state<StudioTab>('ai');
  /** Phones show one pane at a time. */
  mobilePane = $state<'preview' | 'edit'>('preview');

  constructor(options: PluginStudioOptions) {
    this.mode = options.mode;
    this.#pluginService = options.pluginService;
    this.#project = options.project;
    this.#pluginId = options.pluginId;
    this.goToPlugins = options.goToPlugins;
    this.openPlugin = options.openPlugin;
    this.draft = new PluginStudioDraft(options.mode);
    this.example = options.exampleKey ? examplePlugins.find(candidate => candidate.key === options.exampleKey) : undefined;
    this.tabs = options.mode === 'example' ? ['about', 'ai', 'source'] : ['ai', 'source', 'details'];
    this.tab = this.tabs[0];
    if (options.mode === 'new') this.loadState = 'ready';
    if (options.mode === 'example') this.#loadExample();
    this.#destroyEffects = $effect.root(() => {
      $effect(() => this.#loadSavedPlugin());
      $effect(() => this.#checkSavedApproval());
    });
  }

  destroy(): void {
    this.#destroyEffects();
  }

  #loadExample() {
    const example = this.example;
    if (!example) {
      this.loadState = 'missing';
      return;
    }
    void example.loadHtml().then(html => {
      this.draft.reset({name: example.name, description: example.description, html});
      this.loadState = 'ready';
    });
  }

  #loadSavedPlugin() {
    if (this.mode !== 'edit' || this.savedPlugin) return;
    const target = this.plugin;
    if (!target) {
      if (this.#pluginService.loaded) this.loadState = 'missing';
      return;
    }
    this.savedPlugin = target;
    void this.#pluginService.getHtml(target).then(result => {
      if (result.result === 'ok') {
        this.draft.reset({name: target.name, description: target.description ?? '', html: result.html});
        this.loadState = 'ready';
      } else {
        this.loadState = result.result;
      }
    });
  }

  /** The name to show: what the user typed, else the example's or plugin's. */
  get name(): string | undefined {
    return this.draft.name.trim() || this.example?.name || this.plugin?.name || undefined;
  }

  get status(): StudioStatus | undefined {
    if (this.added) return 'added';
    if (this.mode === 'example') return this.draft.customized ? 'example-customized' : 'example';
    if (this.mode === 'new') return 'new';
    return this.draft.dirty ? 'unsaved' : undefined;
  }

  get sizeKb(): number {
    return Math.max(1, Math.round(this.draft.html.length / 1024));
  }

  // ---- Preview ----

  /** Edit mode: compare against the saved version without losing the new one. */
  showSavedVersion = $state(false);
  readonly canCompareVersions = $derived.by(() => this.mode === 'edit' && this.draft.htmlChanged && isCompletePluginHtml(this.draft.baseline.html));
  readonly previewDoc = $derived.by(() => {
    if (this.showSavedVersion && this.canCompareVersions) {
      return {html: this.draft.baseline.html, manifest: parsePluginManifest(this.draft.baseline.html)};
    }
    if (!this.draft.manifest) return undefined;
    return {html: this.draft.html, manifest: this.draft.manifest};
  });

  // Edit mode: the saved version only runs with the run consent the Run page asks for, since another
  // manager may have synced in code that never ran on this device. Versions pasted here are the
  // user's own and don't ask (beyond the internet prompt).
  savedApproval = $state<{hash: string; approved: boolean}>();
  #checkSavedApproval() {
    if (this.mode !== 'edit' || !this.savedPlugin || this.loadState !== 'ready') return;
    const target = this.savedPlugin;
    const html = this.draft.baseline.html;
    this.savedApproval = undefined;
    void computePluginHash({name: target.name, description: target.description, html}).then(hash => {
      if (this.savedPlugin !== target || this.draft.baseline.html !== html) return; // a newer save owns it
      this.savedApproval = {hash, approved: this.#pluginService.consentStore.isGranted(target.id, hash)};
    });
  }
  readonly savedUnapproved = $derived.by(() => this.mode === 'edit' && this.savedApproval?.approved !== true);
  readonly needsConsent = $derived.by(() => this.savedUnapproved && this.previewDoc?.html === this.draft.baseline.html);

  approveSaved(): void {
    if (!this.savedPlugin || !this.savedApproval) return;
    this.#pluginService.consentStore.grant(this.savedPlugin.id, this.savedApproval.hash);
    this.savedApproval = {...this.savedApproval, approved: true};
  }

  // ---- Ask AI ----
  // Kept here rather than in the panels: a tab's panel unmounts when another tab is picked.

  aiDescription = $state('');
  showPromptOptions = $state(false);
  // These only steer the generated prompt; they don't restrict what a finished plugin can do.
  readonly promptOptions = $state<PluginPromptOptions>({
    projectSpecific: true,
    mobile: true,
    allowEdits: true,
    internet: false,
    culturalSensitivity: false,
  });
  changeText = $state('');

  // ---- Prompts for changing the plugin ----

  buildingChangePrompt = $state(false);

  /** Asks for changes to a version (the current one by default), with the setup body a new plugin gets. */
  async buildChangePrompt(change: string, html = this.draft.html): Promise<string> {
    this.buildingChangePrompt = true;
    try {
      const body = await buildPluginPrompt(this.#project.api, {
        projectName: this.#project.projectName,
        projectCode: this.#project.projectCode,
      }, {internet: parsePluginManifest(html).permissions.includes('internet')});
      return `${body}\n\n${pluginChangeSection(html, change)}`;
    } finally {
      this.buildingChangePrompt = false;
    }
  }

  async downloadChangePrompt(change: string, html = this.draft.html): Promise<void> {
    downloadPrompt(await this.buildChangePrompt(change, html), this.draft.name);
  }

  #downloadRemovalPrompt(permission: PluginPermission, html = this.draft.html): Promise<void> {
    // Addressed to the AI, so English like the rest of the prompt.
    const consequence = permission === 'internet'
      ? 'The plugin must work fully offline.'
      : 'The plugin must only read the dictionary, never change it.';
    return this.downloadChangePrompt(`Remove the \`${permission}\` permission from the \`fwlite-plugin-permissions\` meta tag and everything that needs it. ${consequence} Keep everything else as it is.`, html);
  }

  // ---- Reviewing a new version ----

  readonly review = $derived.by(() => {
    const baseHtml = this.draft.reviewBaseHtml;
    if (!this.draft.manifest || !baseHtml || baseHtml === this.draft.html || !isCompletePluginHtml(baseHtml)) return undefined;
    return {
      changes: compareManifests(parsePluginManifest(baseHtml), this.draft.manifest),
      sizeBefore: Math.max(1, Math.round(baseHtml.length / 1024)),
    };
  });
  /** New permissions the user chose to keep, so their warning goes away. */
  keptPermissions = $state<PluginPermission[]>([]);
  followUpDownloaded = $state(false);
  readonly newPermissions = $derived.by(() => (this.review?.changes.added.permissions ?? []).filter(permission => !this.keptPermissions.includes(permission)));

  keepPermission(permission: PluginPermission): void {
    this.keptPermissions = [...this.keptPermissions, permission];
  }

  async askToRemove(permission: PluginPermission): Promise<void> {
    await this.#downloadRemovalPrompt(permission);
    this.followUpDownloaded = true;
  }

  /** Saving a new version drops "Always allow" (see PluginService.update); say so up front. */
  get writeTrustResets(): boolean {
    return this.mode === 'edit' && this.draft.htmlChanged && !!this.savedPlugin
      && this.#pluginService.writeTrustStore.grantedHash(this.savedPlugin.id) !== undefined;
  }

  undoVersion(): void {
    this.draft.undoVersion();
    this.keptPermissions = [];
  }

  // ---- Using a pasted or uploaded version ----

  pasteText = $state('');
  pasteError = $state<'incomplete'>();
  pasteNote = $state<'follow-up-downloaded'>();
  /** A name taken from the plugin's <title> keeps following new versions until the user types one. */
  nameFromHtml = $state(false);

  /**
   * Asked before a version that newly wants the internet runs: the preview runs it at once, and
   * once online it could send project data anywhere. A version that already had the internet
   * (the saved plugin, an example, or one allowed earlier here) doesn't ask again.
   */
  internetPrompt = $state<{html: string; resolve: (answer: 'allow' | 'cancel') => void}>();

  #confirmInternet(html: string): Promise<'allow' | 'cancel'> {
    if (!parsePluginManifest(html).permissions.includes('internet')) return Promise.resolve('allow');
    // An unapproved saved version hasn't been allowed anything yet, so it doesn't count.
    const currentAllowed = !(this.savedUnapproved && this.draft.html === this.draft.baseline.html);
    if (this.draft.manifest?.permissions.includes('internet') && currentAllowed) return Promise.resolve('allow');
    return new Promise(resolve => this.internetPrompt = {html, resolve});
  }

  answerInternetPrompt(answer: 'allow' | 'cancel'): void {
    this.internetPrompt?.resolve(answer);
    this.internetPrompt = undefined;
  }

  async askToRemoveInternetFirst(): Promise<void> {
    const prompt = this.internetPrompt;
    if (!prompt) return;
    await this.#downloadRemovalPrompt('internet', prompt.html);
    this.answerInternetPrompt('cancel');
    this.pasteNote = 'follow-up-downloaded';
  }

  /** Makes a pasted reply or uploaded file the current version, if it is a plugin (and allowed). */
  async useHtml(candidate: string): Promise<boolean> {
    const html = extractPluginHtml(candidate);
    if (!isCompletePluginHtml(html)) {
      this.pasteError = 'incomplete';
      return false;
    }
    this.pasteError = undefined;
    this.pasteNote = undefined;
    const hadInternet = !!this.draft.manifest?.permissions.includes('internet');
    if (await this.#confirmInternet(html) === 'cancel') return false;
    this.draft.applyVersion(html);
    this.showSavedVersion = false;
    // Allowed just now, so the review card lists it instead of warning about it again.
    this.keptPermissions = !hadInternet && this.draft.manifest?.permissions.includes('internet') ? ['internet'] : [];
    this.followUpDownloaded = false;
    if (this.mode === 'new' && (!this.draft.name.trim() || this.nameFromHtml)) {
      const title = pluginHtmlTitle(html);
      if (title) {
        this.draft.name = title;
        this.nameFromHtml = true;
      }
    }
    this.mobilePane = 'preview';
    return true;
  }

  async applyPaste(): Promise<void> {
    if (await this.useHtml(this.pasteText)) this.pasteText = '';
  }

  // ---- Saving ----

  saving = $state(false);

  #draftToSave() {
    return {name: this.draft.name.trim(), description: this.draft.description.trim() || undefined, html: this.draft.html};
  }

  async addToProject(): Promise<IPlugin> {
    this.saving = true;
    try {
      this.added = this.mode === 'example' && this.example
        ? await this.#pluginService.addExample(this.example, this.#draftToSave(), this.draft.customized)
        : await this.#pluginService.create(this.#draftToSave());
      return this.added;
    } finally {
      this.saving = false;
    }
  }

  async saveEdit(): Promise<void> {
    if (!this.savedPlugin) return;
    this.saving = true;
    try {
      const saved = this.#draftToSave();
      const updated = await this.#pluginService.update(this.savedPlugin, saved, this.draft.baseline.html);
      this.savedPlugin = updated;
      this.draft.reset({name: updated.name, description: updated.description ?? '', html: saved.html});
    } finally {
      this.saving = false;
    }
  }

  async removePlugin(): Promise<void> {
    if (!this.savedPlugin) return;
    await this.#pluginService.delete(this.savedPlugin.id);
    this.added = this.savedPlugin; // nothing left to lose
    this.goToPlugins();
  }

  discardChanges(): void {
    this.draft.reset(this.draft.baseline);
    this.nameFromHtml = false;
  }

  // ---- Leaving ----

  readonly hasUnsavedWork = $derived.by(() => !this.added && this.draft.dirty);
  leavePrompt = $state<(leave: boolean) => void>();

  confirmLeave(): Promise<boolean> {
    if (!this.hasUnsavedWork) return Promise.resolve(true);
    return new Promise(resolve => this.leavePrompt = resolve);
  }

  answerLeavePrompt(leave: boolean): void {
    this.leavePrompt?.(leave);
    this.leavePrompt = undefined;
  }

  /** Back to the Plugins page, asking first if there's unsaved work. */
  async leave(): Promise<void> {
    if (await this.confirmLeave()) this.goToPlugins();
  }
}

export const [useStudio, setStudio] = createContext<PluginStudio>();
