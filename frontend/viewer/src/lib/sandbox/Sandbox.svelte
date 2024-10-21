<script lang="ts">
  import {setContext} from 'svelte';
  import {Button, type MenuOption} from 'svelte-ux';
  import {readable, type Readable} from 'svelte/store';
  import MultiOptionEditor from '../entry-editor/field-editors/MultiOptionEditor.svelte';
  import CrdtMultiOptionField from '../entry-editor/inputs/CrdtMultiOptionField.svelte';
  import {type View} from '../entry-editor/view-data';
  import MapBind from '../utils/MapBind.svelte';
  import {initWritingSystems} from '../writing-systems';

  initWritingSystems(readable({
    analysis: [{
      id: 'test',
      name: 'test',
      abbreviation: 'test',
      font: 'test',
      exemplars: [],
    }],
    vernacular: [],
  }));

  const fields = {
    'multi1': {show: true, order: 1},
    'multi2': {show: true, order: 2},
    'multi3': {show: true, order: 3},
  };
  const fieldGridAreas = Object.keys(fields).map(field => `'${field}'`).join(' ');

  setContext<Readable<View>>('currentView', readable({
    id: 'sandbox',
    i18nKey: 'languageForge',
    label: 'Language Forge',
    fields,
  }));

  const options = [
    { label: 'One', id: '1' },
    { label: 'Two', id: '2' },
    { label: 'Three', id: '3' },
    { label: 'Four', id: '4' },
  ];

  function findOption(id: string) {
    return options.find(o => o.id === id)!;
  }

  let idValue = ['3'];
  let idObjectValue = [{id: '3'}];
  let optionValue = [{ label: 'Three', id: '3' }];

  const crdtOptions: MenuOption[] = [
    {value: 'a', label: 'Alpha'},
    {value: 'b', label: 'Beta'},
    {value: 'c', label: 'Charlie'},
  ];

  let crdtValue = ['a'];
</script>

<MapBind bind:in={idValue} bind:out={idObjectValue} map={(value) => value.map(v => ({id: v}))} unmap={(value => value.map(v => v.id))} />
<MapBind bind:in={idObjectValue} bind:out={optionValue} map={(value) => value.map(v => findOption(v.id))} unmap={(value => value)} />

<div class="grid grid-cols-3 gap-6 p-6">
  <div class="flex flex-col gap-2 border p-4 justify-between">
    MultiOptionEditor configurations
    <div class="grid gap-2" style:grid-template-areas={fieldGridAreas}>
      <MultiOptionEditor id="multi1" name="String values" bind:value={idValue} idValues getOptionLabel={(o) => o.label} wsType="analysis" readonly={false} {options} />
      <MultiOptionEditor id="multi2" name="(id: string) values" bind:value={idObjectValue} getValueById={findOption} getOptionLabel={(o) => o.label} wsType="analysis" readonly={false} {options} />
      <MultiOptionEditor id="multi3" name="Option values" bind:value={optionValue} getOptionLabel={(o) => o.label} wsType="analysis" readonly={false} {options} />
    </div>
    <div class="flex flex-col">
      <p>selected: {idValue.join('|')}</p>
      <Button variant="fill" on:click={() => idValue = ['4']}>Select Four only</Button>
    </div>
  </div>
  <div class="flex flex-col gap-2 border p-4 justify-between">
    <div class="flex flex-col gap-2">
      Lower level editor
      <div class="mb-4">
        String values and MenuOptions
        <CrdtMultiOptionField bind:value={crdtValue} options={crdtOptions}/>
      </div>
    </div>
    <div class="flex flex-col">
      <p>selected: {crdtValue.join('|')}</p>
      <Button variant="fill" on:click={() => crdtValue = ['c']}>Select Charlie only</Button>
    </div>
  </div>
</div>
