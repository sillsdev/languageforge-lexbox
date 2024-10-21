import {type Writable, writable} from 'svelte/store';
import {type View, views} from '../entry-editor/view-data';
import {getContext, onDestroy, setContext} from 'svelte';

const currentViewContextName = 'currentView';
const viewSettingsContextName = 'viewSettings';

export interface ViewSettings {
  hideEmptyFields: boolean;
}

export function initViewSettings(defaultSettings: ViewSettings): Writable<ViewSettings> {
  //todo load from local storage
  const viewSettingsStore = writable<ViewSettings>(defaultSettings);
  setContext<Writable<ViewSettings>>(viewSettingsContextName, viewSettingsStore);
  return viewSettingsStore;
}

export function initView(defaultView: View): Writable<View> {
  const localView = localStorage.getItem('currentView');
  const currentViewStore = writable<View>(views.find(v => v.id === localView) ?? defaultView);
  onDestroy(currentViewStore.subscribe(v => localStorage.setItem('currentView', v.id)));
  setContext<Writable<View>>(currentViewContextName, currentViewStore);
  return currentViewStore;
}

/**
 * Current view contains details of the currently selected view, this is used to determine which fields are shown
 * and what labels are used.
 */
export function useCurrentView(): Writable<View> {
  return getContext<Writable<View>>(currentViewContextName);
}

/**
 * View settings contains user specified settings, such as hiding empty fields.
 */
export function useViewSettings(): Writable<ViewSettings> {
  return getContext<Writable<ViewSettings>>(viewSettingsContextName);
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
    .toSorted((a, b) => a[1].order - b[1].order)
    .map(([id, _field]) => `"${id} ${id} ${id}"`).join(' ');
}
