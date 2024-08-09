import {type Writable, writable} from 'svelte/store';
import type {View} from '../entry-editor/view-data';
import {getContext, setContext} from 'svelte';

const currentViewContextName = 'currentView';
const viewSettingsContextName = 'viewSettings';

export interface ViewSettings {
  hideEmptyFields: boolean;
}

export function initViewSettings(defaultSettings: ViewSettings) {
  //todo load from local storage
  const viewSettingsStore = writable<ViewSettings>(defaultSettings);
  setContext<Writable<ViewSettings>>(viewSettingsContextName, viewSettingsStore);
  return viewSettingsStore;
}
export function initView(defaultView: View) {
  //todo load from local storage
  const currentViewStore = writable<View>(defaultView);
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
