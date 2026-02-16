import {get, type Writable, writable} from 'svelte/store';
import type {FieldId} from '$lib/entry-editor/field-data';
import {
  allFields,
  ENTRY_FIELD_IDS,
  EXAMPLE_FIELD_IDS,
  FW_CLASSIC_VIEW,
  FW_LITE_VIEW,
  SENSE_FIELD_IDS,
  type FieldView,
  type Overrides,
  type View,
  views
} from './view-data';
import {getContext, onDestroy, setContext} from 'svelte';
import {useProjectContext, type ProjectContext} from '$project/project-context.svelte';
import {ViewBase, type ICustomView, type IViewField} from '$lib/dotnet-types';
import {UserProjectRole} from '$lib/dotnet-types/generated-types/LcmCrdt/UserProjectRole';
import {watch} from 'runed';
import type {IMiniLcmJsInvokable} from '$lib/dotnet-types';

const currentViewContextName = 'currentView';
const viewSettingsContextName = 'viewSettings';
const viewsContextName = 'views';
const customViewIdPrefix = 'custom:';

export type CustomViewInput = {
  label: string;
  baseViewId: 'fwlite' | 'fieldworks';
  fieldIds: FieldId[];
  overrides?: Overrides;
};

export type CustomViewWriteDeps = {
  viewsStore: Writable<View[]>;
  currentView: Writable<View>;
  projectContext: ProjectContext;
};

export function isCustomView(view: View): view is View & { parentView: View } {
  return typeof view === 'object' && view !== null && 'parentView' in view;
}

function apiCustomViewIdToViewId(id: string): string {
  return `${customViewIdPrefix}${id}`;
}

function viewIdToApiCustomViewId(id: string): string {
  if (!id.startsWith(customViewIdPrefix)) {
    throw new Error(`View ${id} is not a custom view id.`);
  }
  return id.slice(customViewIdPrefix.length);
}

export function fieldIdsFromView(view: View): FieldId[] {
  return (Object.keys(view.fields) as FieldId[])
    .filter((id) => view.fields[id].show)
    .sort((a, b) => view.fields[a].order - view.fields[b].order);
}

function assertCanManageCustomViews(projectContext: ProjectContext) {
  if (projectContext.projectData?.role !== UserProjectRole.Manager) {
    throw new Error('Only managers can manage custom views.');
  }
}

export function useCustomViewWriteDeps(): CustomViewWriteDeps {
  return {
    viewsStore: useViews(),
    currentView: useCurrentView(),
    projectContext: useProjectContext(),
  };
}

function getFieldIdsFromApi(customView: ICustomView): FieldId[] {
  const orderedFields = [
    ...(customView.entryFields ?? []),
    ...(customView.senseFields ?? []),
    ...(customView.exampleFields ?? []),
  ];
  return orderedFields.map((f) => f.fieldId).filter((id): id is FieldId => id in allFields);
}

function splitFieldIdsByEntity(fieldIds: FieldId[]): { entryFields: IViewField[]; senseFields: IViewField[]; exampleFields: IViewField[] } {
  function toViewField(fieldId: FieldId): IViewField {
    return { fieldId } satisfies IViewField;
  }
  const entryFieldIds = new Set<FieldId>(ENTRY_FIELD_IDS);
  const senseFieldIds = new Set<FieldId>(SENSE_FIELD_IDS);
  const exampleFieldIds = new Set<FieldId>(EXAMPLE_FIELD_IDS);
  return {
    entryFields: fieldIds.filter((fieldId) => entryFieldIds.has(fieldId)).map(toViewField),
    senseFields: fieldIds.filter((fieldId) => senseFieldIds.has(fieldId)).map(toViewField),
    exampleFields: fieldIds.filter((fieldId) => exampleFieldIds.has(fieldId)).map(toViewField),
  };
}

function buildCustomViewFromApi(customView: ICustomView): View {
  const parentView = customView.base === ViewBase.FieldWorks ? FW_CLASSIC_VIEW : FW_LITE_VIEW;
  const fieldIds = getFieldIdsFromApi(customView);

  const fields = Object.fromEntries((Object.keys(allFields) as FieldId[]).map((id) => {
    const show = fieldIds.includes(id);
    const order = show ? (fieldIds.indexOf(id) + 1) : allFields[id].order;
    return [id, { show, order } satisfies FieldView];
  })) as Record<FieldId, FieldView>;

  const overrides: Overrides = {
    vernacularWritingSystems: customView.vernacular,
    analysisWritingSystems: customView.analysis,
  };
  const overridesOrUndefined = (overrides.analysisWritingSystems || overrides.vernacularWritingSystems) ? overrides : undefined;

  return {
    id: apiCustomViewIdToViewId(customView.id),
    type: parentView.type,
    label: customView.name,
    fields,
    parentView,
    overrides: overridesOrUndefined,
  };
}

export function initViews(_updateLocalStorage: boolean = true): Writable<View[]> {
  const initialViews: View[] = [
    FW_LITE_VIEW,
    FW_CLASSIC_VIEW,
  ];
  const viewsStore = writable<View[]>(initialViews);

  // Load custom views from MiniLcm API when available.
  // ProjectContext queues the load until the API is set up.
  try {
    const projectContext = useProjectContext();
    const customViewsResource = projectContext.apiResource<ICustomView[]>([], (api: IMiniLcmJsInvokable) => {
      const getCustomViews = api.getCustomViews.bind(api) as () => Promise<ICustomView[]>;
      return getCustomViews();
    });
    watch(
      () => customViewsResource.current,
      (apiViews) => {
        const builtFromApi = (apiViews ?? [])
          .filter((v) => !v.deletedAt)
          .map(buildCustomViewFromApi);
        viewsStore.update((current) => {
          const existingCustom = current.filter(isCustomView);
          const apiIds = new Set(builtFromApi.map((v) => v.id));
          const keepExisting = existingCustom.filter((v) => !apiIds.has(v.id));
          return [FW_LITE_VIEW, FW_CLASSIC_VIEW, ...builtFromApi, ...keepExisting];
        });
      }
    );
  } catch {
    // No project context (e.g. in unit tests). Keep only built-in views.
  }

  setContext<Writable<View[]>>(viewsContextName, viewsStore);
  return viewsStore;
}

export function useViews(): Writable<View[]> {
  const viewsStore = getContext<Writable<View[]>>(viewsContextName);
  if (!viewsStore) throw new Error('Views are not initialized. Did you forget to call initViews()?');
  return viewsStore;
}

export function tryUseViews(): Writable<View[]> | undefined {
  return getContext<Writable<View[]>>(viewsContextName);
}

export async function addCustomView(args: CustomViewInput, deps: CustomViewWriteDeps): Promise<View> {
  const {viewsStore, projectContext} = deps;
  assertCanManageCustomViews(projectContext);
  const api: IMiniLcmJsInvokable = projectContext.api;
  const splitFields = splitFieldIdsByEntity(args.fieldIds);

  const newCustomView: ICustomView = {
    id: '00000000-0000-0000-0000-000000000000',
    name: args.label,
    base: args.baseViewId === 'fieldworks' ? ViewBase.FieldWorks : ViewBase.FwLite,
    entryFields: splitFields.entryFields,
    senseFields: splitFields.senseFields,
    exampleFields: splitFields.exampleFields,
    vernacular: args.overrides?.vernacularWritingSystems,
    analysis: args.overrides?.analysisWritingSystems,
  };
  const createCustomView = api.createCustomView.bind(api) as (customView: ICustomView) => Promise<ICustomView>;
  const created = await createCustomView(newCustomView);

  const view = buildCustomViewFromApi(created);
  viewsStore.update((current) => {
    if (current.some((v) => v.id === view.id)) return current;
    return [...current, view];
  });
  return view;
}

export async function updateCustomView(viewId: string, args: CustomViewInput, deps: CustomViewWriteDeps): Promise<View> {
  const {viewsStore, currentView, projectContext} = deps;
  assertCanManageCustomViews(projectContext);
  const api: IMiniLcmJsInvokable = projectContext.api;
  const splitFields = splitFieldIdsByEntity(args.fieldIds);
  const apiId = viewIdToApiCustomViewId(viewId);

  const updatedCustomView: ICustomView = {
    id: apiId,
    name: args.label,
    base: args.baseViewId === 'fieldworks' ? ViewBase.FieldWorks : ViewBase.FwLite,
    entryFields: splitFields.entryFields,
    senseFields: splitFields.senseFields,
    exampleFields: splitFields.exampleFields,
    vernacular: args.overrides?.vernacularWritingSystems,
    analysis: args.overrides?.analysisWritingSystems,
  };

  const updateApi = api.updateCustomView.bind(api) as (id: string, customView: ICustomView) => Promise<ICustomView>;
  const updated = await updateApi(apiId, updatedCustomView);
  const nextView = buildCustomViewFromApi(updated);

  viewsStore.update((current) => current.map((view) => view.id === nextView.id ? nextView : view));

  if (get(currentView).id === nextView.id) {
    currentView.set(nextView);
  }

  return nextView;
}

export async function deleteCustomView(viewId: string, deps: CustomViewWriteDeps): Promise<void> {
  const {viewsStore, currentView, projectContext} = deps;
  assertCanManageCustomViews(projectContext);
  const api: IMiniLcmJsInvokable = projectContext.api;
  const apiId = viewIdToApiCustomViewId(viewId);

  const deletedView = get(viewsStore).find((view) => view.id === viewId);
  if (deletedView && !isCustomView(deletedView)) {
    throw new Error(`Cannot delete non-custom view ${viewId}.`);
  }

  const deleteApi = api.deleteCustomView.bind(api) as (id: string) => Promise<void>;
  await deleteApi(apiId);

  viewsStore.update((current) => current.filter((view) => view.id !== viewId));

  if (deletedView && isCustomView(deletedView)) {
    if (get(currentView).id === viewId) {
      currentView.set(deletedView.parentView);
    }
  }
}

export interface ViewSettings {
  showEmptyFields: boolean;
}

export function initViewSettings(defaultSettings: ViewSettings = {showEmptyFields: true}): Writable<ViewSettings> {
  //todo load from local storage
  const viewSettingsStore = writable<ViewSettings>(defaultSettings);
  setContext<Writable<ViewSettings>>(viewSettingsContextName, viewSettingsStore);
  return viewSettingsStore;
}

export function initView(defaultView?: View, updateLocalStorage: boolean = true): Writable<View> {
  const availableViews = get(tryUseViews() ?? writable<View[]>(views));
  defaultView ??= availableViews[0];
  const localView = localStorage.getItem('currentView');
  const currentViewStore = writable<View>(availableViews.find(v => v.id === localView) ?? defaultView);
  if (updateLocalStorage)
    onDestroy(currentViewStore.subscribe(v => localStorage.setItem('currentView', v.id)));
  setContext<Writable<View>>(currentViewContextName, currentViewStore);
  return currentViewStore;
}

/**
 * Current view contains details of the currently selected view, this is used to determine which fields are shown
 * and what labels are used.
 */
export function useCurrentView(): Writable<View> {
  const currentView = getContext<Writable<View>>(currentViewContextName);
  if (!currentView) throw new Error('Current view is not initialized. Are you in the context of a project?');
  return currentView;
}

/**
 * View settings contains user specified settings, such as hiding empty fields.
 */
export function useViewSettings(): Writable<ViewSettings> {
  const viewSettings = getContext<Writable<ViewSettings>>(viewSettingsContextName);
  if (!viewSettings) throw new Error('View settings is not initialized. Are you in the context of a project?');
  return viewSettings;
}

/**
 * Returns a string that can be used as a grid-template-areas value for a grid with the given fields, ordered based on the current view.
 * looks like `"lexemeForm lexemeForm lexemeForm" "citationForm citationForm citationForm" "literalMeaning literalMeaning literalMeaning"` etc. each group of fields in quotes is a row.
 * there are 3 columns for each row and one field per row so the field name is repeated 3 times.
 */
export function objectTemplateAreas(view: View, obj: object | string[]): string {
  const fields = Array.isArray(obj) ? obj : Object.keys(obj);
  return Object.entries(view.fields)
    .filter(([id, field]) => fields.includes(id) && field.show)
    .sort((a, b) => a[1].order - b[1].order)
    .map(([id, _field]) => `"${id} ${id} ${id}"`).join(' ');
}
