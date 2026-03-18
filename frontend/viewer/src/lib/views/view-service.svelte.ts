import {getContext, setContext} from 'svelte';
import {ViewBase} from '$lib/dotnet-types';
import {useCustomViewService, type CustomViewService} from '$project/data/custom-view-service.svelte';
import {BUILT_IN_VIEWS, FW_CLASSIC_VIEW, FW_LITE_VIEW, type RootView, type TypedViewField, type View} from './view-data';
import type {FieldId} from './entity-config';

const contextKey = Symbol('view-service');

export function initViewService(options?: {persist?: boolean}): ViewService {
  const service = new ViewService(useCustomViewService(), options);
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
  /** Persisted ID enables correct lazy initialization of async-loaded custom views. */
  #selectedId = $state<string | null>(null);
  /** Transient override view (e.g. from OverrideFields), not in the views list. */
  #override = $state<View | null>(null);
  #persist: boolean;

  views: View[] = $derived.by(() => ([...BUILT_IN_VIEWS, ...this.#customViewService.current]));

  currentView: View = $derived.by(() => {
    if (this.#override) return this.#override;
    const found = this.views.find(v => v.id === this.#selectedId);
    return found ?? this.views[0];
  });

  rootView: RootView = $derived(
    this.currentView.base === ViewBase.FieldWorks ? FW_CLASSIC_VIEW : FW_LITE_VIEW
  );

  constructor(customViewService: CustomViewService, options?: {persist?: boolean}) {
    this.#customViewService = customViewService;
    this.#persist = options?.persist ?? true;
    this.#selectedId = localStorage.getItem('currentView') ?? null;
  }

  /** Select a view from the known views list by ID. */
  selectView(viewId: string): void {
    if (!this.views.some(v => v.id === viewId)) {
      throw new Error(`Cannot select view with id ${viewId} - not found in views list.`);
    }

    this.#override = null;
    this.#selectedId = viewId;
    if (this.#persist) {
      localStorage.setItem('currentView', viewId);
    }
  }

  /** Set a transient override view that isn't in the views list. */
  overrideView(view: View): void {
    this.#override = view;
  }
}

// Pure utilities — no instance needed

export function hasVisibleFields(fields: TypedViewField<FieldId>[]): boolean {
  return fields.some((field) => field.show);
}

export function objectTemplateAreas(fields: TypedViewField<FieldId>[]): string {
  return fields
    .filter((field) => field.show)
    .map((field) => `"${field.fieldId} ${field.fieldId} ${field.fieldId}"`)
    .join(' ');
}
