import {getContext, setContext} from 'svelte';
import {ViewBase, type ICustomView, type IViewField} from '$lib/dotnet-types';
import {useCustomViewService, type CustomViewService} from '$project/data/custom-view-service.svelte';
import {BUILT_IN_VIEWS, FW_CLASSIC_VIEW, FW_LITE_VIEW, type CustomView, type RootView, type TypedViewField, type View} from './view-data';
import type {FieldId} from './entity-config';
import {type ProjectStorage, useProjectStorage} from '$lib/storage/project-storage.svelte';

const contextKey = Symbol('view-service');

export function initViewService(options?: {persist?: boolean}): ViewService {
  const projectStorage = useProjectStorage();
  const service = new ViewService(useCustomViewService(), projectStorage, options);
  setContext(contextKey, service);
  return service;
}

export function useViewService(): ViewService {
  const service = getContext<ViewService>(contextKey);
  if (!service) throw new Error('ViewService not initialized. Did you forget to call initViewService()?');
  return service;
}

export class ViewService {
  #customViewService: CustomViewService;
  #projectStorage: ProjectStorage;
  /** Persisted ID enables correct lazy initialization of async-loaded custom views. */
  #selectedId = $state<string | null>(null);
  /** Transient override view (e.g. from OverrideFields), not in the views list. */
  #override = $state<View | null>(null);
  #persist: boolean;

  views: View[] = $derived.by(() => ([
    ...BUILT_IN_VIEWS,
    ...this.#customViewService.current.map(v => fromApiCustomView(v)),
  ]));

  currentView: View = $derived.by(() => {
    if (this.#override) return this.#override;
    const id = this.#selectedId || this.#projectStorage.currentView.current;
    const found = this.views.find(v => v.id === id);
    return found ?? this.views[0];
  });

  rootView: RootView = $derived(
    this.currentView.base === ViewBase.FieldWorks ? FW_CLASSIC_VIEW : FW_LITE_VIEW
  );

  constructor(customViewService: CustomViewService, projectStorage: ProjectStorage, options?: {persist?: boolean}) {
    this.#customViewService = customViewService;
    this.#persist = options?.persist ?? true;
    this.#projectStorage = projectStorage;
  }

  /** Select a view from the known views list by ID. */
  selectView(viewId: string): void {
    if (!this.views.some(v => v.id === viewId)) {
      throw new Error(`Cannot select view with id ${viewId} - not found in views list.`);
    }

    this.#override = null;
    this.#selectedId = viewId;
    if (this.#persist) {
      void this.#projectStorage.currentView.set(viewId);
    }
  }

  /** Set a transient override view that isn't in the views list. */
  overrideView(view: View): void {
    this.#override = view;
  }
}

export function hasVisibleFields(fields: TypedViewField<FieldId>[]): boolean {
  return fields.some((field) => field.show);
}

/**
 * Returns a string that can be used as a grid-template-areas value for a grid with the given fields, ordered based on the current view.
 * looks like `"lexemeForm lexemeForm lexemeForm" "citationForm citationForm citationForm" "literalMeaning literalMeaning literalMeaning"` etc. each group of fields in quotes is a row.
 * there are 3 columns for each row and one field per row so the field name is repeated 3 times.
 */
export function objectTemplateAreas(fields: TypedViewField<FieldId>[]): string {
  return fields
    .filter((field) => field.show)
    .map((field) => `"${field.fieldId} ${field.fieldId} ${field.fieldId}"`)
    .join(' ');
}

function fromApiCustomView(customView: ICustomView): CustomView {
  const baseView = structuredClone(customView.base === ViewBase.FieldWorks ? FW_CLASSIC_VIEW : FW_LITE_VIEW);
  return {
    ...customView,
    custom: true,
    entryFields: resolveViewFields(customView.entryFields, baseView.entryFields),
    senseFields: resolveViewFields(customView.senseFields, baseView.senseFields),
    exampleFields: resolveViewFields(customView.exampleFields, baseView.exampleFields),
  };
}

/** Applies API visibility and ordering to the default fields, backfilling any missing fields as hidden. */
function resolveViewFields<T extends FieldId>(apiFields: IViewField[] | undefined, defaults: TypedViewField<T>[]): TypedViewField<T>[] {
  if (!apiFields) return defaults.map((f) => ({...f}));
  return defaults.map((f) => ({...f, show: !!apiFields.find(_f => _f.fieldId === f.fieldId)}));
}

export function toApiViewFields(fields: TypedViewField<FieldId>[]): IViewField[] {
  return fields.filter(f => f.show).map(f => ({fieldId: f.fieldId}));
}
