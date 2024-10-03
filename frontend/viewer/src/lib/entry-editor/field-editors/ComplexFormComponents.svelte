<script lang="ts">
  import FieldTitle from '../FieldTitle.svelte';
  import { useCurrentView } from '../../services/view-service';
  import EntryOrSensePicker, { type EntrySenseSelection } from './EntryOrSensePicker.svelte';
  import { headword, randomId } from '../../utils';
  import { createEventDispatcher } from 'svelte';
  import type { IComplexFormComponent } from '../../mini-lcm/i-complex-form-component';
  import EntryOrSenseItemList from './EntryOrSenseItemList.svelte';
  import { Button } from 'svelte-ux';
  import { mdiPlus } from '@mdi/js';

  const dispatch = createEventDispatcher<{
    change: { value: IComplexFormComponent[] };
  }>();

  export let id: string;
  export let name: string | undefined = undefined;
  export let value: IComplexFormComponent[];
  export let readonly: boolean;
  export let entryId: string;
  let currentView = useCurrentView();

  $: empty = !value;

  let openPicker = false;

  function addComponent(selection: EntrySenseSelection) {
    const component = {
      id: randomId(),
      complexFormEntryId: entryId,
      componentEntryId: selection.entry.id,
      componentSenseId: selection.sense?.id,
      componentHeadword: headword(selection.entry),
    };
    value = [...value, component];
    dispatch('change', { value });
  }
</script>

<div
  class="complex-form-components-field field"
  class:empty
  class:hidden={!$currentView.fields[id].show}
  style:grid-area={id}
>
  <FieldTitle {id} {name} />
  <EntryOrSenseItemList bind:value {readonly} on:change={(e) => dispatch('change', { value })} getEntryId={(e) => e.componentEntryId} getHeadword={(e) => e.componentHeadword}>
    <svelte:fragment slot="actions">
      <Button on:click={() => openPicker = true} icon={mdiPlus} variant="fill-light" color="success" size="sm">
        <div class="max-sm:hidden">Add Component</div>
      </Button>
      <EntryOrSensePicker title="Add component to complex form" bind:open={openPicker} on:pick={(e) => addComponent(e.detail)}
        disableEntry={(e) => e.id === entryId || value.some((c) => c.componentEntryId === e.id && !c.componentSenseId)}
        disableSense={(s, e) => value.some((c) => c.componentEntryId === e.id && c.componentSenseId === s.id)} />
    </svelte:fragment>
  </EntryOrSenseItemList>
</div>
