<script lang="ts">
  import {defaultEntry} from '../utils';
  import {mdiHistory} from '@mdi/js';
  import {Button, Dialog, Toggle} from 'svelte-ux';
  import type {IEntry} from '../mini-lcm';
  import EntryEditor from '../entry-editor/EntryEditor.svelte';
  import {getContext} from 'svelte';

  export let id: string;
  let loading = false;
  let snapshot: { entity: IEntry, typeName: 'Entity' };
  let projectName = getContext<string>('project-name')

  async function load() {
    loading = true;
    const data = await fetch(`/api/${projectName}/history/snapshot/${id}`).then(res => res.json());
    snapshot = data;
    loading = false;
  }
</script>
<Toggle let:on={open} let:toggleOn let:toggleOff on:toggleOn={load}>
  <Button on:click={toggleOn} icon={mdiHistory} variant="fill" color="info" size="sm">
    View History
  </Button>
  <Dialog {open} on:close={toggleOff} {loading} persistent={loading} class="w-[700px]">
    <div slot="title">History</div>
    <div class="m-6">
<!--      todo add snapshot list-->
      {#if snapshot?.entity}
        <EntryEditor entry={snapshot.entity} modalMode/>
      {/if}
    </div>
    <div class="flex-grow"></div>
  </Dialog>
</Toggle>
