<script lang="ts">

  import {Toggle, Button, Dialog} from 'svelte-ux';
  import EntryEditor from './EntryEditor.svelte';
  import type {IEntry} from '../mini-lcm';
  import {useLexboxApi} from '../services/service-provider';
  import { mdiPlus } from '@mdi/js';

  let loading = false;
  let entry: IEntry = defaultEntry();

  function defaultEntry(): IEntry {
    return {
      id: crypto.randomUUID(),
      citationForm: {},
      lexemeForm: {},
      note: {},
      literalMeaning: {},
      senses: []
    };
  }
  const lexboxApi = useLexboxApi();

  async function createEntry(e: Event, closeDialog: () => void) {
    e.preventDefault();
    loading = true;
    await lexboxApi.CreateEntry(entry)
    loading = false;
    closeDialog();
  }
</script>
<Toggle let:on={open} let:toggleOn let:toggleOff>
  <Button on:click={toggleOn} icon={mdiPlus} variant="fill-outline" color="success" size="sm">New Entry</Button>
  <Dialog {open} on:close={toggleOff} {loading} persistent={loading} class="w-[700px]">
    <div slot="title">New Entry</div>
    <div class="m-6">
      <EntryEditor {entry}/>
    </div>
    <div class="flex-grow"></div>
    <div slot="actions">
      <Button>Cancel</Button>
      <Button variant="fill-light" color="success" on:click={e => createEntry(e, toggleOff)}>Create Entry</Button>
    </div>
  </Dialog>
</Toggle>
