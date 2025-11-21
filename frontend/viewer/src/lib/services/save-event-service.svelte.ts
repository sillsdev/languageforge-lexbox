import {useProjectContext} from '$project/project-context.svelte';

export type SaveEvent = { saving: true } | { saved: true } | { status: 'saved-to-disk' | 'failed-to-save' };

const symbol = Symbol.for('fw-lite-save-event-service');
export function useSaveHandler(): SaveHandler {
  const projectContext = useProjectContext();
  return projectContext.getOrAdd(symbol, () => new SaveHandler());
}

export class SaveHandler {
  currentEvent: SaveEvent = $state({ status: 'saved-to-disk'});
  async handleSave<T>(saveAction: () => Promise<T> ): Promise<T> {
    this.currentEvent = { saving: true };
    let threw = false;
    try {
      return await saveAction();
    } catch (e) {
      this.currentEvent = { status: 'failed-to-save' };
      threw = true;
      throw e;
    } finally {
      if (!threw)
        this.currentEvent = { saved: true };
    }
  }
}
