import type {IEntry} from '$lib/dotnet-types';
import {derived, get, type Readable} from 'svelte/store';

/**
 * Returns a store like selectedEntry, but only emits when a completely different entry is selected (or no entry is selected),
 * rather than when the currently selected entry experiences a change (which may result in selectedEntry emitting).
 */
export function getSelectedEntryChangedStore(selectedEntry: Readable<IEntry | undefined>): Readable<IEntry | undefined> {
  let previousSelectedEntry = get(selectedEntry);
  return derived(selectedEntry, ($selectedEntry, set) => {
    if (previousSelectedEntry?.id !== $selectedEntry?.id) {
      previousSelectedEntry = $selectedEntry;
      set($selectedEntry);
    }
  });
}
