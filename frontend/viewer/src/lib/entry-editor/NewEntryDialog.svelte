<script lang="ts">
  import {Toggle, Button, Dialog} from 'svelte-ux';
  import EntryEditor from './object-editors/EntryEditor.svelte';
  import type {IEntry} from '$lib/dotnet-types';
  import {useLexboxApi} from '../services/service-provider';
  import { mdiBookPlusOutline } from '@mdi/js';
  import { defaultEntry } from '../utils';
  import { createEventDispatcher, getContext } from 'svelte';
  import type { SaveHandler } from '../services/save-event-service';
  import {useCurrentView} from '$lib/services/view-service';
  import {fieldName} from '$lib/i18n';

  const dispatch = createEventDispatcher<{
    created: { entry: IEntry };
  }>();

  let loading = false;
  let entry: IEntry = defaultEntry();

  const currentView = useCurrentView();
  const lexboxApi = useLexboxApi();
  const saveHandler = getContext<SaveHandler>('saveHandler');
  let requester: {
    resolve: (entry: IEntry | undefined) => void
  } | undefined;

  async function createEntry(e: Event, closeDialog: () => void) {
    e.preventDefault();
    loading = true;
    console.debug('Creating entry', entry);
    await saveHandler(() => lexboxApi.createEntry(entry));
    dispatch('created', {entry});
    if (requester) {
      requester.resolve(entry);
      requester = undefined;
    }
    loading = false;
    closeDialog();
  }

  export function openWithValue(newEntry: Partial<IEntry>): Promise<IEntry | undefined> {
    return new Promise<IEntry | undefined>((resolve) => {
      if (requester) requester.resolve(undefined);
      requester = { resolve };
      const tmpEntry = defaultEntry();
      toggle.on = true;
      entry = {...tmpEntry, ...newEntry, id: tmpEntry.id};
    });
  }

  function onClosing() {
    if (requester) {
      requester.resolve(undefined);
      requester = undefined;
    }
    entry = defaultEntry();
  }

  let toggle: Toggle;
</script>

<Toggle bind:this={toggle} let:on={open} let:toggleOn let:toggleOff on:toggleOff={onClosing}>
  <Button on:click={toggleOn} icon={mdiBookPlusOutline} variant="fill-outline" color="success" size="sm">
    <div class="hidden sm:contents">
      New {fieldName({id: 'entry'}, $currentView.i18nKey)}
    </div>
  </Button>
  <Dialog {open} on:close={toggleOff} {loading} persistent={loading}>
    <div slot="title">New {fieldName({id: 'entry'}, $currentView.i18nKey)}</div>
    <div class="m-6">
      <EntryEditor bind:entry={entry} modalMode/>
    </div>
    <div class="flex-grow"></div>
    <div slot="actions">
      <Button>Cancel</Button>
      <Button variant="fill-light" color="success" on:click={e => createEntry(e, toggleOff)}>Create {fieldName({id: 'entry'}, $currentView.i18nKey)}</Button>
    </div>
  </Dialog>
</Toggle>
