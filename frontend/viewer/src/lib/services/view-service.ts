import {type Writable, writable} from 'svelte/store';
import type {View} from '../entry-editor/view-data';
import {getContext, setContext} from 'svelte';

const currentViewContextName = 'currentView';
const viewSettingsContextName = 'viewSettings';

export interface ViewSettings {
  hideEmptyFields: boolean;
}

export function initViewSettings(settings: ViewSettings) {
  const viewSettingsStore = writable<ViewSettings>(settings);
  setContext<Writable<ViewSettings>>(viewSettingsContextName, viewSettingsStore);
  return viewSettingsStore;
}
export function initView(view: View) {
  const currentViewStore = writable<View>(view);
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
