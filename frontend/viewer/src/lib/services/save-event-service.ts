import { writable, type Readable, type Writable } from 'svelte/store';

export type SaveEvent = { saving: true } | { saved: true } | { status: 'saved-to-disk' | 'failed-to-save' };
export type SaveEventEmmiter = Readable<SaveEvent>;
type SaveEventDispatcher = Writable<SaveEvent>;
export type SaveHandler = <T>(saveAction: () => Promise<T>) => Promise<T>;
export const saveEventDispatcher: SaveEventDispatcher = writable<SaveEvent>({ status: 'saved-to-disk' });
// eslint-disable-next-line func-style
export const saveHandler: SaveHandler = async <T>(saveAction: () => Promise<T>): Promise<T> => {
  saveEventDispatcher.set({ saving: true });
  try {
    const result = await saveAction();
    saveEventDispatcher.set({ saved: true });
    return result;
  } catch (e) {
    saveEventDispatcher.set({ status: 'failed-to-save' });
    throw e;
  }
};
