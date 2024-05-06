<script lang="ts">

  import {Toggle, Button, Dialog} from 'svelte-ux';
  import EntryEditor from './EntryEditor.svelte';
  import type {IEntry} from '../mini-lcm';
  import {useLexboxApi} from '../services/service-provider';

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
  <Button on:click={toggleOn} variant="fill-outline">Create Entry</Button>
  <Dialog {open} on:close={toggleOff} {loading} persistent={loading}>
    <div slot="title">New Entry</div>
    <div class="m-6">
      <EntryEditor {entry}/>
    </div>
    <div slot="actions">
      <Button variant="fill" color="primary" on:click={e => createEntry(e, toggleOff)}>Save Entry</Button>
      <Button>Cancel</Button>
    </div>
  </Dialog>
</Toggle>
