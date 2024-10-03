<script lang="ts">
  import FieldTitle from '../FieldTitle.svelte';
  import { useCurrentView } from '../../services/view-service';
  import EntryOrSensePicker, { type EntrySenseSelection } from '../EntryOrSensePicker.svelte';
  import { headword, randomId } from '../../utils';
  import { createEventDispatcher } from 'svelte';
  import type { IComplexFormComponent } from '../../mini-lcm/i-complex-form-component';
  import EntryOrSenseItemList from '../EntryOrSenseItemList.svelte';
  import { Button } from 'svelte-ux';
  import { mdiPlus } from '@mdi/js';
  import type { IEntry } from '../../mini-lcm';

  const dispatch = createEventDispatcher<{
    change: { value: IComplexFormComponent[] };
  }>();

  export let id: string;
  export let name: string | undefined = undefined;
  export let value: IComplexFormComponent[];
  export let readonly: boolean;
  export let entry: IEntry;
  let currentView = useCurrentView();

  $: empty = !value?.length;

  let openPicker = false;

  function addComplexForm(selection: EntrySenseSelection) {
    const complexForm = {
      id: randomId(),
      complexFormEntryId: selection.entry.id,
      complexFormHeadword: headword(selection.entry),
      componentEntryId: entry.id,
    };
    value = [...value, complexForm];
    dispatch('change', { value });
  }

$: disabledEntriesForPicker = [
  entry.id,
  ...entry.components.map((c) => c.componentEntryId),
  ...entry.complexForms.map((c) => c.componentEntryId),
];
</script>

<div
  class="complex-forms-field field"
  class:empty
  class:hidden={!$currentView.fields[id].show}
  style:grid-area={id}
>
  <FieldTitle {id} {name} />
  <EntryOrSenseItemList bind:value {readonly} on:change={(e) => dispatch('change', { value })} getEntryId={(e) => e.complexFormEntryId} getHeadword={(e) => e.complexFormHeadword}>
    <svelte:fragment slot="actions">
      <Button on:click={() => openPicker = true} icon={mdiPlus} variant="fill-light" color="success" size="sm">
        <div class="max-sm:hidden">Add Complex Form</div>
      </Button>
      <EntryOrSensePicker title="Add complex form" bind:open={openPicker} noSenses on:pick={(e) => addComplexForm(e.detail)}
        disableEntry={(e) => disabledEntriesForPicker.includes(e.id)}
        disableSense={(s, e) => value.some((c) => c.componentEntryId === e.id && c.componentSenseId === s.id)} />
    </svelte:fragment>
  </EntryOrSenseItemList>
</div>
