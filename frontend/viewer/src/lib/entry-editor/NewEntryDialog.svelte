<script lang="ts">
  import {Toggle, Button, Dialog} from 'svelte-ux';
  import EntryEditor from './object-editors/EntryEditor.svelte';
  import type {IEntry} from '../mini-lcm';
  import {useLexboxApi} from '../services/service-provider';
  import { mdiBookPlusOutline } from '@mdi/js';
  import { defaultEntry } from '../utils';
  import { createEventDispatcher, getContext } from 'svelte';
  import type { SaveHandler } from '../services/save-event-service';

  const dispatch = createEventDispatcher<{
    created: { entry: IEntry};
  }>();

  let loading = false;
  let entry: IEntry = defaultEntry();

  const lexboxApi = useLexboxApi();
  const saveHandler = getContext<SaveHandler>('saveHandler');

  async function createEntry(e: Event, closeDialog: () => void) {
    e.preventDefault();
    loading = true;
    await saveHandler(() => lexboxApi.CreateEntry(entry));
    dispatch('created', {entry});
    loading = false;
    closeDialog();
  }

  export function openWithValue(newEntry: Partial<IEntry>) {
    const tmpEntry = defaultEntry();
    toggle.on = true;
    entry = {...tmpEntry, ...newEntry, id: tmpEntry.id};
  }
  let toggle: Toggle;
</script>

<Toggle bind:this={toggle} let:on={open} let:toggleOn let:toggleOff on:toggleOff={() => entry = defaultEntry()}>
  <Button on:click={toggleOn} icon={mdiBookPlusOutline} variant="fill-outline" color="success" size="sm">
    <div class="hidden sm:contents">
      New Entry
    </div>
  </Button>
  <Dialog {open} on:close={toggleOff} {loading} persistent={loading} class="w-[700px]">
    <div slot="title">New Entry</div>
    <div class="m-6">
      <EntryEditor bind:entry={entry} modalMode/>
    </div>
    <div class="flex-grow"></div>
    <div slot="actions">
      <Button>Cancel</Button>
      <Button variant="fill-light" color="success" on:click={e => createEntry(e, toggleOff)}>Create Entry</Button>
    </div>
  </Dialog>
</Toggle>
