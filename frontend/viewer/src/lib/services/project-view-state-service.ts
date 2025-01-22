import {getContext, setContext} from 'svelte';
import {type Readable, type Writable, writable} from 'svelte/store';

const projectViewStateContextName = 'project-view-state';

export type ProjectViewState = {
  /**
   * Tracks whether the user has explicitly collapsed the right toolbar.
   * Used to inform the toolbar contents, so it can behave accordingly.
   * 🤔 I suppose this could largely be handled by css container queries.
   */
  rightToolbarCollapsed: boolean,
  /**
   * Tracks whether the user has explicitly picked an entry to view.
   * (whether via the entry list, an entry-ID in the URL, creating a new entry etc.)
   * Used on smaller screen e.g. mobile to determine whether to show the entry list or the entry editor.
   */
  userPickedEntry: boolean,
};

export function initProjectViewState(defaultFeatures: ProjectViewState): Writable<ProjectViewState> {
  const projectViewStateStore = writable<ProjectViewState>(defaultFeatures);
  setContext<Readable<ProjectViewState>>(projectViewStateContextName, projectViewStateStore);
  return projectViewStateStore;
}

export function useProjectViewState(): Readable<ProjectViewState> {
  return getContext<Readable<ProjectViewState>>(projectViewStateContextName);
}
