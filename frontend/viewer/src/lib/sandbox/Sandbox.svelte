<script lang="ts">
  import {Button, type MenuOption, MultiSelectField,} from 'svelte-ux';
  import CrdtMultiOptionField from '../entry-editor/CrdtMultiOptionField.svelte';

  const options = [
    { name: 'One', value: 1 },
    { name: 'Two', value: 2 },
    { name: 'Three', value: 3 },
    { name: 'Four', value: 4 },
  ];

  let value = [3];

  const crdtOptions:MenuOption[] = [
    {value: 'a', label: 'Alpha'},
    {value: 'b', label: 'Beta'},
    {value: 'c', label: 'Charlie'},
  ];

  let crdtValue = [{id: 'a'}];
</script>

<div class="grid gap-3 justify-items-start">
  <MultiSelectField {options} {value} on:change={(e) => (value = e.detail.value)}/>
  <p>selected: {value.join('|')}</p>
  <Button on:click={() => value = [4]}>Select Four only</Button>
</div>
<div class="grid gap-3 justify-items-start">
  <CrdtMultiOptionField bind:value={crdtValue} options={crdtOptions}/>
  <p>selected: {crdtValue.map(c => c.id).join('|')}</p>
  <Button on:click={() => crdtValue = [{id: 'c'}]}>Select Charlie only</Button>
</div>
