import {type IPlugin} from '$lib/dotnet-types';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import type {DetachedResourceReturn} from '$project/detached-resource';
import {parsePluginContexts} from '$lib/plugins/plugin-srcdoc';
import type {PluginContext} from '$lib/plugins/plugin-api-types';

const symbol = Symbol.for('fw-lite-plugin-service');

export function usePluginService(): PluginService {
  const projectContext = useProjectContext();
  return projectContext.getOrAdd(symbol, () => new PluginService(projectContext));
}

export class PluginService {
  #projectContext: ProjectContext;
  #pluginsResource: DetachedResourceReturn<IPlugin[]>;

  constructor(projectContext: ProjectContext) {
    this.#projectContext = projectContext;
    this.#pluginsResource = projectContext.apiResource([], (api) => api.getPlugins());
  }

  current: IPlugin[] = $derived.by(() =>
    this.#pluginsResource.current
      .toSorted((a, b) => a.name.localeCompare(b.name))
  );

  // A plugin's declared contexts only change when its HTML does, but parsing that (whole) HTML is
  // costly — so parse each plugin once here rather than per consumer (e.g. once per entry-menu open).
  #contexts: Map<string, PluginContext[]> = $derived.by(() =>
    new Map(this.#pluginsResource.current.map(plugin => [plugin.id, parsePluginContexts(plugin.html)]))
  );

  /** Plugins that declared they belong in the given context, e.g. an entry's menu. */
  pluginsForContext(context: PluginContext): IPlugin[] {
    return this.current.filter(plugin => this.#contexts.get(plugin.id)?.includes(context) ?? false);
  }

  get loading(): boolean {
    return this.#pluginsResource.loading;
  }

  /** True once the plugin list has been fetched at least once. */
  get loaded(): boolean {
    return this.#pluginsResource.loaded;
  }

  async refetch(): Promise<void> {
    await this.#pluginsResource.refetch();
  }

  async add(plugin: IPlugin): Promise<IPlugin> {
    const created = await this.#projectContext.api.createPlugin(plugin);
    await this.#pluginsResource.refetch();
    return created;
  }

  async update(plugin: IPlugin): Promise<IPlugin> {
    const updated = await this.#projectContext.api.updatePlugin(plugin);
    await this.#pluginsResource.refetch();
    return updated;
  }

  async delete(pluginId: string): Promise<void> {
    await this.#projectContext.api.deletePlugin(pluginId);
    await this.#pluginsResource.refetch();
  }
}
