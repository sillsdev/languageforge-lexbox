<script lang="ts">
  import FieldTitle from '../FieldTitle.svelte';
  import { useCurrentView } from '$lib/views/view-service';
  import EntryOrSensePicker, { type EntrySenseSelection } from '../EntryOrSensePicker.svelte';
  import { makeHasHadValueTracker, randomId } from '$lib/utils';
  import { createEventDispatcher } from 'svelte';
  import EntryOrSenseItemList from '../EntryOrSenseItemList.svelte';
  import { Button } from 'svelte-ux';
  import { mdiPlus } from '@mdi/js';
  import type { IEntry, ISense, IComplexFormComponent } from '$lib/dotnet-types';
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
  const writingSystemService = useWritingSystemService();

  let hasHadValueTracker = makeHasHadValueTracker();
  let hasHadValue = hasHadValueTracker.store;
  $: hasHadValueTracker.pushAndGet(value?.length);

  let openPicker = false;

  function addComponent(selection: EntrySenseSelection) {
    const component: IComplexFormComponent = {
      id: randomId(),
      complexFormEntryId: entry.id,
      complexFormHeadword: writingSystemService.headword(entry),
      componentEntryId: selection.entry.id,
      componentSenseId: selection.sense?.id,
      componentHeadword: writingSystemService.headword(selection.entry),
    };
    value = [...value, component];
    dispatch('change', { value });
  }

  function disableEntry(e: IEntry): false | { disableSenses?: true, reason: string } {
    if (e.id === entry.id) return { reason: 'Current Entry', disableSenses: true };
    if (entry.components.some((c) => c.componentEntryId === e.id && !c.componentSenseId)) return { reason: 'Component' };
    if (entry.complexForms.some((cf) => cf.complexFormEntryId === e.id)) return { reason: 'Complex Form', disableSenses: true };
    return false;
  }

  function disableSense(s: ISense, e: IEntry): false | string {
    if (entry.components.some((c) => c.componentEntryId === e.id && c.componentSenseId === s.id)) return 'Component';
    return false;
  }
</script>

<div
  class="complex-form-components-field field"
  class:unused={!$hasHadValue}
  class:hidden={!$currentView.fields[id].show}
  style:grid-area={id}
>
  <FieldTitle {id} {name} />
  <div class="item-list-field">
    <EntryOrSenseItemList bind:value {readonly} orderable on:change={() => dispatch('change', { value })} getEntryId={(e) => e.componentEntryId} getHeadword={(e) => e.componentHeadword}>
      <svelte:fragment slot="actions">
        <Button on:click={() => openPicker = true} icon={mdiPlus} variant="fill-light" color="success" size="sm">
          Add Component
        </Button>
        <EntryOrSensePicker title="Add component to complex form" bind:open={openPicker} on:pick={(e) => addComponent(e.detail)}
          {disableEntry} {disableSense} />
      </svelte:fragment>
    </EntryOrSenseItemList>
  </div>
</div>
