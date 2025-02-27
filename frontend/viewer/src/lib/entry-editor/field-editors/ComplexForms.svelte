<script lang="ts">
  import FieldTitle from '../FieldTitle.svelte';
  import { useCurrentView } from '$lib/views/view-service';
  import EntryOrSensePicker, { type EntrySenseSelection } from '../EntryOrSensePicker.svelte';
  import { makeHasHadValueTracker, randomId } from '$lib/utils';
  import { createEventDispatcher } from 'svelte';
  import EntryOrSenseItemList from '../EntryOrSenseItemList.svelte';
  import { Button } from 'svelte-ux';
  import { mdiPlus } from '@mdi/js';
  import type { IEntry, IComplexFormComponent } from '$lib/dotnet-types';
  import {useWritingSystemService} from '$lib/writing-system-service';

  const dispatch = createEventDispatcher<{
    change: { value: IComplexFormComponent[] };
  }>();

  export let id: string;
  export let name: string | undefined = undefined;
  export let value: IComplexFormComponent[];
  export let readonly: boolean;
  export let entry: IEntry;
  let currentView = useCurrentView();
  let writingSystemService = useWritingSystemService();

  let hasHadValueTracker = makeHasHadValueTracker();
  let hasHadValue = hasHadValueTracker.store;
  $: hasHadValueTracker.pushAndGet(value?.length);

  let openPicker = false;

  function addComplexForm(selection: EntrySenseSelection) {
    const complexForm: IComplexFormComponent = {
      id: randomId(),
      complexFormEntryId: selection.entry.id,
      complexFormHeadword: writingSystemService.headword(selection.entry),
      componentEntryId: entry.id,
      componentHeadword: writingSystemService.headword(entry),
    };
    value = [...value, complexForm];
    dispatch('change', { value });
  }

  function disableEntry(e: IEntry): false | { reason: string, andSenses?: true } {
    if (e.id === entry.id) return { reason: 'Current Entry' };
    if (entry.components.some((c) => c.componentEntryId === e.id)) return { reason: 'Component' };
    if (entry.complexForms.some((cf) => cf.complexFormEntryId === e.id)) return { reason: 'Complex Form' };
    return false;
  }
</script>

<div
  class="complex-forms-field field"
  class:unused={!$hasHadValue}
  class:hidden={!$currentView.fields[id].show}
  style:grid-area={id}
>
  <FieldTitle {id} {name} />
  <div class="item-list-field">
    <EntryOrSenseItemList bind:value {readonly} on:change={() => dispatch('change', { value })} getEntryId={(e) => e.complexFormEntryId} getHeadword={(e) => e.complexFormHeadword}>
      <svelte:fragment slot="actions">
        <Button on:click={() => openPicker = true} icon={mdiPlus} variant="fill-light" color="success" size="sm">
          Add Complex Form
        </Button>
        <EntryOrSensePicker title="Add complex form" bind:open={openPicker} mode="only-entries" on:pick={(e) => addComplexForm(e.detail)}
          {disableEntry} />
      </svelte:fragment>
    </EntryOrSenseItemList>
  </div>
</div>
