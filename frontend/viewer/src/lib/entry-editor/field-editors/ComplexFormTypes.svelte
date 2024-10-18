<script lang="ts">
  import FieldTitle from '../FieldTitle.svelte';
  import { useCurrentView } from '../../services/view-service';
  import { firstVal } from '../../utils';
  import { createEventDispatcher } from 'svelte';
  import { Button } from 'svelte-ux';
  import { mdiPlus } from '@mdi/js';
  import type { IComplexFormType, } from '../../mini-lcm';
  import OrderedItemList from '../OrderedItemList.svelte';

  const dispatch = createEventDispatcher<{
    change: { value: IComplexFormType[] };
  }>();

  export let id: string;
  export let name: string | undefined = undefined;
  export let value: IComplexFormType[];
  export let readonly: boolean;
  let currentView = useCurrentView();

  $: empty = !value?.length;

  // let openPicker = false;

  // function addComplexFormType(selection: MultiString) {
  //   const complexFormType = {
  //     id: randomId(),
  //     name: selection,
  //   };
  //   value = [...value, complexFormType];
  //   dispatch('change', { value });
  // }
</script>

<div
  class="complex-forms-field field"
  class:empty
  class:hidden={!$currentView.fields[id].show}
  style:grid-area={id}
>
  <FieldTitle {id} {name} />

  <div class="item-list-field">
    <OrderedItemList bind:value {readonly} on:change={() => dispatch('change', { value })} getDisplayName={(type) => firstVal(type.name)}>
      <svelte:fragment slot="actions">
        <Button disabled on:click={() => {/* openPicker = true */}} icon={mdiPlus} variant="fill-light" color="success" size="sm">
          <div class="max-sm:hidden">Add Complex Form Type</div>
        </Button>
        <!-- TODO: implement form type picker or dropdown? <EntryOrSensePicker title="Add complex form" bind:open={openPicker} noSenses on:pick={(e) => addComplexFormType(e.detail)} /> -->
      </svelte:fragment>
    </OrderedItemList>
  </div>
</div>
