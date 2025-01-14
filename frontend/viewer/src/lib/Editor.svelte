<script lang="ts">
  import type {IComplexFormComponent, IEntry, IExampleSentence, ISense} from '$lib/dotnet-types';
  import EntryEditor from './entry-editor/object-editors/EntryEditor.svelte';
  import {createEventDispatcher, getContext} from 'svelte';
  import {useLexboxApi} from './services/service-provider';
  import type { SaveHandler } from './services/save-event-service';
  import {useViewSettings} from './services/view-service';

  const lexboxApi = useLexboxApi();
  const saveHandler = getContext<SaveHandler>('saveHandler');

  const dispatch = createEventDispatcher<{
    delete: { entry: IEntry };
    change: { entry: IEntry };
    refreshEntry: { entryId: IEntry['id'] };
    refreshEntries: { entryIds: IEntry['id'][] };
  }>();

  export let entry: IEntry;
  export let readonly: boolean;
  $: initialEntry = JSON.parse(JSON.stringify(entry)) as IEntry;

  function updateInitialEntry() {
    // eslint-disable-next-line svelte/no-reactive-reassign
    initialEntry = JSON.parse(JSON.stringify(entry)) as IEntry;
  }

  const viewSettings = useViewSettings();

  function listHasChanged(entryList: IComplexFormComponent[], initialList: IComplexFormComponent[]): boolean {
    if (entryList && !initialList) return true;
    if (!entryList && initialList) return true;
    if (!entryList && !initialList) return false;
    // By this point we know that both lists exist
    if (entryList.length != initialList.length) return true;
    for (var i = 0; i < entryList.length; i++) {
      if (entryList[i] != initialList[i]) return true;
    }
    return false;
  }

  async function onChange(e: { entry: IEntry, sense?: ISense, example?: IExampleSentence }) {
    if (readonly) return;
    await updateEntry(e.entry);
    dispatch('change', {entry: e.entry});
    if (listHasChanged(e.entry.complexForms, initialEntry.complexForms)) {
      const entryIdsWithDupes = [
        ...e.entry.complexForms.map(c => c.complexFormEntryId),
        ...initialEntry.complexForms.map(c => c.complexFormEntryId),
      ];
      const entryIds = [...new Set(entryIdsWithDupes)];
      dispatch('refreshEntries', { entryIds });
    }
    if (listHasChanged(e.entry.components, initialEntry.components)) {
      const entryIdsWithDupes = [
        ...e.entry.components.map(c => c.componentEntryId),
        ...initialEntry.components.map(c => c.componentEntryId),
      ];
      const entryIds = [...new Set(entryIdsWithDupes)];
      dispatch('refreshEntries', { entryIds });
    }
    updateInitialEntry();
  }

  async function onDelete(e: { entry: IEntry, sense?: ISense, example?: IExampleSentence }) {
    if (readonly) return;
    if (e.example !== undefined && e.sense !== undefined) {
      await saveHandler(() => lexboxApi.deleteExampleSentence(e.entry.id, e.sense!.id, e.example!.id));
    } else if (e.sense !== undefined) {
      await saveHandler(() => lexboxApi.deleteSense(e.entry.id, e.sense!.id));
    } else {
      await saveHandler(() => lexboxApi.deleteEntry(e.entry.id));
      dispatch('delete', {entry: e.entry});
      return;
    }
    updateInitialEntry();
  }

  async function updateEntry(updatedEntry: IEntry) {
    if (entry.id != updatedEntry.id) throw new Error('Entry id mismatch');
    console.debug('Updating entry', updatedEntry);
    await saveHandler(() => lexboxApi.updateEntry(initialEntry, updatedEntry));
  }
</script>

<div id="entry" class:hide-empty={$viewSettings.hideEmptyFields}>
  <EntryEditor
    on:change={e => onChange(e.detail)}
    on:delete={e => onDelete(e.detail)}
    entry={entry}
    {readonly}/>
</div>

<style lang="postcss">
  :global(.hide-empty :is(.empty, .ws-field-wrapper:has(.empty))) {
    display: none !important;
  }
</style>
