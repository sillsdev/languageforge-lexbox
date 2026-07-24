import {type ICustomView} from '$lib/dotnet-types';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import type {ResourceReturn} from 'runed';

const symbol = Symbol.for('fw-lite-custom-view-service');

export function useCustomViewService(): CustomViewService {
  const projectContext = useProjectContext();
  return projectContext.getOrAdd(symbol, () => new CustomViewService(projectContext));
}

export class CustomViewService {
  #projectContext: ProjectContext;
  #customViewsResource: ResourceReturn<ICustomView[], unknown, true>;
  /** Avoid GetCustomViews on browse first paint — built-ins cover the default path. */
  #loadRequested = $state(false);

  constructor(projectContext: ProjectContext) {
    this.#projectContext = projectContext;
    this.#customViewsResource = projectContext.apiResource([], async (api) => {
      if (!this.#projectContext.features.customViews) return [];
      return api.getCustomViews();
    });
  }

  /** Fetch custom views if not already requested (ViewPicker, manage dialog, or restored selection). */
  ensureLoaded(): void {
    this.#loadRequested = true;
  }

  current: ICustomView[] = $derived.by(() => {
    if (!this.#loadRequested) return [];
    if (!this.#projectContext.features.customViews) return [];
    return this.#customViewsResource.current
      .toSorted((a, b) => a.name.localeCompare(b.name));
  });

  async add(customView: ICustomView): Promise<ICustomView> {
    this.ensureLoaded();
    const created = await this.#projectContext.api.createCustomView(customView);
    await this.#customViewsResource.refetch();
    return created;
  }

  async update(customView: ICustomView): Promise<ICustomView> {
    this.ensureLoaded();
    const updated = await this.#projectContext.api.updateCustomView(customView);
    await this.#customViewsResource.refetch();
    return updated;
  }

  async delete(viewId: string): Promise<void> {
    this.ensureLoaded();
    await this.#projectContext.api.deleteCustomView(viewId);
    await this.#customViewsResource.refetch();
  }
}
