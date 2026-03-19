import {ViewBase, type ICustomView, type IViewField} from '$lib/dotnet-types';
import type {FieldId} from '$lib/views/entity-config';
import {
  FW_CLASSIC_VIEW,
  FW_LITE_VIEW, type CustomView,
  type TypedViewField,
  type View
} from '$lib/views/view-data';
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

  current: CustomView[] = $derived.by(() =>
    this.#customViewsResource.current
      .map((v) => this.#buildCustomView(v))
      .toSorted((a, b) => a.name.localeCompare(b.name))
  );

  async add(customView: View): Promise<CustomView> {
    const created = await this.#projectContext.api.createCustomView(this.#toApiCustomView(customView));
    await this.#customViewsResource.refetch();
    return this.#buildCustomView(created);
  }

  async update(viewId: string, customView: View): Promise<CustomView> {
    const updated = await this.#projectContext.api.updateCustomView(viewId, this.#toApiCustomView(customView));
    await this.#customViewsResource.refetch();
    return this.#buildCustomView(updated);
  }

  async delete(viewId: string): Promise<void> {
    await this.#projectContext.api.deleteCustomView(viewId);
    await this.#customViewsResource.refetch();
  }

  #buildCustomView(customView: ICustomView): CustomView {
    const baseView = structuredClone(customView.base === ViewBase.FieldWorks ? FW_CLASSIC_VIEW : FW_LITE_VIEW);
    return {
      ...customView,
      custom: true,
      entryFields: this.#resolveViewFields(customView.entryFields, baseView.entryFields),
      senseFields: this.#resolveViewFields(customView.senseFields, baseView.senseFields),
      exampleFields: this.#resolveViewFields(customView.exampleFields, baseView.exampleFields),
    };
  }

  #toApiCustomView(customView: View): ICustomView {
    return {
      ...customView,
      entryFields: customView.entryFields.filter(field => field.show),
      senseFields: customView.senseFields.filter(field => field.show),
      exampleFields: customView.exampleFields.filter(field => field.show),
    };
  }

  /** Applies API visibility and ordering to the default fields, backfilling any missing fields as hidden. */
  #resolveViewFields<T extends FieldId>(apiFields: IViewField[] | undefined, defaults: TypedViewField<T>[]): TypedViewField<T>[] {
    if (!apiFields) return defaults.map((f) => ({...f}));
    return defaults.map((f) => ({...f, show: !!apiFields.find(_f => _f.fieldId === f.fieldId)}));
  }
}
