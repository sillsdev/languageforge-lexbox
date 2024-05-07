<script lang="ts">

  import {Toggle, Button, Dialog} from 'svelte-ux';
  import EntryEditor from './EntryEditor.svelte';
  import type {IEntry} from '../mini-lcm';
  import {useLexboxApi} from '../services/service-provider';
  import { mdiBookPlusOutline } from '@mdi/js';
  import { defaultEntry } from '../utils';
  import { createEventDispatcher } from 'svelte';

  const dispatch = createEventDispatcher<{
    created: { entry: IEntry};
  }>();

  let loading = false;
  let entry: IEntry = defaultEntry();

  const lexboxApi = useLexboxApi();

  async function createEntry(e: Event, closeDialog: () => void) {
    e.preventDefault();
    loading = true;
    await lexboxApi.CreateEntry(entry);
    dispatch('created', {entry});
    loading = false;
    closeDialog();
  }
</script>

<Toggle let:on={open} let:toggleOn let:toggleOff on:toggleOn={() => entry = defaultEntry()}>
  <Button on:click={toggleOn} icon={mdiBookPlusOutline} variant="fill-outline" color="success" size="sm">New Entry</Button>
  <Dialog {open} on:close={toggleOff} {loading} persistent={loading} class="w-[700px]">
    <div slot="title">New Entry</div>
    <div class="m-6">
      <EntryEditor {entry} modalMode/>
    </div>
    <div class="flex-grow"></div>
    <div slot="actions">
      <Button>Cancel</Button>
      <Button variant="fill-light" color="success" on:click={e => createEntry(e, toggleOff)}>Create Entry</Button>
    </div>
  </Dialog>
</Toggle>
