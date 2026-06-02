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

  constructor(projectContext: ProjectContext) {
    this.#projectContext = projectContext;
    this.#customViewsResource = projectContext.apiResource([], (api) => api.getCustomViews());
  }

  current: ICustomView[] = $derived.by(() =>
    this.#customViewsResource.current
      .toSorted((a, b) => a.name.localeCompare(b.name))
  );

  async add(customView: ICustomView): Promise<ICustomView> {
    const created = await this.#projectContext.api.createCustomView(customView);
    await this.#customViewsResource.refetch();
    return created;
  }

  async update(customView: ICustomView): Promise<ICustomView> {
    const updated = await this.#projectContext.api.updateCustomView(customView);
    await this.#customViewsResource.refetch();
    return updated;
  }

  async delete(viewId: string): Promise<void> {
    await this.#projectContext.api.deleteCustomView(viewId);
    await this.#customViewsResource.refetch();
  }
}
