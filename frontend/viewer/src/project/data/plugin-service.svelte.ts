import {type IPlugin} from '$lib/dotnet-types';
import {gt} from 'svelte-i18n-lingui';
import {ReadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/ReadFileResult';
import {UploadFileResult} from '$lib/dotnet-types/generated-types/MiniLcm/Media/UploadFileResult';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import type {DetachedResourceReturn} from '$project/detached-resource';
import {parsePluginManifest} from '$lib/plugins/plugin-manifest';
import type {PluginContext} from '$lib/plugins/plugin-api-types';
import {
  computePluginHash,
  hexFromBytes,
  PluginConsentStore,
  PluginWriteTrustStore,
} from '$lib/plugins/plugin-local-data';

const symbol = Symbol.for('fw-lite-plugin-service');
const textEncoder = new TextEncoder();

/** ASCII-only so the name survives every file system and hg; non-Latin names fall back to "plugin". */
export function fileNameSlug(name: string): string {
  return name.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 40) || 'plugin';
}

export function usePluginService(): PluginService {
  const projectContext = useProjectContext();
  return projectContext.getOrAdd(symbol, () => new PluginService(projectContext));
}

/** What the editor submits; everything else on the Plugin entity is derived from the HTML. */
export interface PluginDraft {
  name: string;
  description?: string;
  html: string;
}

export type PluginHtmlResult =
  | {result: 'ok'; html: string}
  | {result: 'offline'}
  | {result: 'error'; message?: string};

export class PluginService {
  #projectContext: ProjectContext;
  #pluginsResource: DetachedResourceReturn<IPlugin[]>;
  readonly consentStore: PluginConsentStore;
  readonly writeTrustStore: PluginWriteTrustStore;

  constructor(projectContext: ProjectContext) {
    this.#projectContext = projectContext;
    this.#pluginsResource = projectContext.apiResource([], (api) => api.getPlugins());
    this.consentStore = new PluginConsentStore(projectContext.projectCode);
    this.writeTrustStore = new PluginWriteTrustStore(projectContext.projectCode);
  }

  current: IPlugin[] = $derived.by(() =>
    this.#pluginsResource.current
      .toSorted((a, b) => a.name.localeCompare(b.name))
  );

  /** Plugins that declared they belong in the given context, e.g. an entry's menu. */
  pluginsForContext(context: PluginContext): IPlugin[] {
    return this.current.filter(plugin => plugin.contexts.includes(context));
  }

  get loading(): boolean {
    return this.#pluginsResource.loading;
  }

  get loaded(): boolean {
    return this.#pluginsResource.loaded;
  }

  async refetch(): Promise<void> {
    await this.#pluginsResource.refetch();
  }

  /** Fetches a plugin's HTML from its media resource (cached locally; downloads on first use). */
  async getHtml(plugin: IPlugin): Promise<PluginHtmlResult> {
    const file = await this.#projectContext.api.getFileStream(plugin.mediaUri, true);
    if (!file.stream) {
      if (file.result === ReadFileResult.Offline) return {result: 'offline'};
      return {result: 'error', message: file.errorMessage ?? file.result};
    }
    const bytes = await file.stream.arrayBuffer();
    return {result: 'ok', html: new TextDecoder().decode(bytes)};
  }

  async create(draft: PluginDraft): Promise<IPlugin> {
    const htmlBytes = textEncoder.encode(draft.html);
    const {plugin, uploadedHtmlDigest} = await this.#buildPlugin(crypto.randomUUID(), draft, htmlBytes);
    const created = await this.#projectContext.api.createPlugin(plugin);
    // The local author has seen (or written) this exact content: creating counts as run consent.
    this.consentStore.grant(created.id, await computePluginHash(draft, {htmlDigest: uploadedHtmlDigest}));
    await this.#pluginsResource.refetch();
    return created;
  }

  /**
   * Saves an edit. When the HTML changes, device-local grants (run consent, write trust) are
   * re-pinned to the new content hash — an edit made HERE is its own consent, while a change
   * that arrives via sync must ask again. Name/description-only edits leave grants alone.
   */
  async update(previous: IPlugin, draft: PluginDraft, previousHtml: string): Promise<IPlugin> {
    const htmlChanged = previousHtml !== draft.html;
    const htmlBytes = htmlChanged ? textEncoder.encode(draft.html) : undefined;
    const {plugin, uploadedHtmlDigest} = await this.#buildPlugin(
      previous.id,
      draft,
      htmlBytes,
      {previous, previousHtml},
    );
    const updated = await this.#projectContext.api.updatePlugin(plugin);

    if (uploadedHtmlDigest) {
      const oldHash = await computePluginHash({
        name: previous.name,
        description: previous.description,
        html: previousHtml,
      });
      const newHash = await computePluginHash(draft, {htmlDigest: uploadedHtmlDigest});
      this.consentStore.grant(updated.id, newHash);
      if (this.writeTrustStore.isGranted(updated.id, oldHash)) {
        this.writeTrustStore.grant(updated.id, newHash);
      }
    }

    await this.#pluginsResource.refetch();
    return updated;
  }

  async delete(plugin: IPlugin): Promise<void> {
    const pluginId = plugin.id;
    await this.#projectContext.api.deletePlugin(pluginId);
    this.consentStore.revoke(pluginId);
    this.writeTrustStore.revoke(pluginId);
    await this.#pluginsResource.refetch();
  }

  /** Uploads changed HTML as a new immutable file; `uploadedHtmlDigest` is set only then. */
  async #buildPlugin(
    id: string,
    draft: PluginDraft,
    htmlBytes: Uint8Array | undefined,
    existing?: {previous: IPlugin; previousHtml: string},
  ): Promise<{plugin: IPlugin; uploadedHtmlDigest?: ArrayBuffer}> {
    const manifest = parsePluginManifest(draft.html);
    let mediaUri = existing?.previous.mediaUri;
    let fileSize = existing?.previous.fileSize;
    let uploadedHtmlDigest: ArrayBuffer | undefined;
    if (!existing || existing.previousHtml !== draft.html) {
      if (!htmlBytes) throw new Error('htmlBytes required when uploading plugin HTML');
      uploadedHtmlDigest = await crypto.subtle.digest('SHA-256', htmlBytes as BufferSource);
      const contentHash = hexFromBytes(uploadedHtmlDigest);
      const response = await this.#projectContext.api.saveFile(htmlBytes, {
        // The hash makes each version a distinct immutable file, so a synced/cached copy can
        // never be stale; the name is for people browsing LinkedFiles.
        filename: `${fileNameSlug(draft.name)}-${contentHash.slice(0, 8)}.html`,
        mimeType: 'text/html',
        extraFields: {},
      });
      if (response.result === UploadFileResult.TooBig) {
        throw new Error(gt`This plugin file is too big (the limit is 10 MB)`);
      }
      if (!response.mediaUri) {
        throw new Error(response.errorMessage ?? 'Failed to save the plugin file');
      }
      mediaUri = response.mediaUri;
      fileSize = htmlBytes.byteLength;
    }
    return {
      uploadedHtmlDigest,
      plugin: {
        id,
        name: draft.name,
        description: draft.description,
        mediaUri: mediaUri!,
        fileSize: fileSize ?? 0,
        permissions: manifest.permissions,
        contexts: manifest.contexts,
        requires: manifest.requires,
      },
    };
  }
}
