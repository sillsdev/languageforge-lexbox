<script lang="ts">
  import {setContext} from 'svelte';
  import {readable, type Readable} from 'svelte/store';
  import type {View} from '$lib/views/view-data';
  import MultiOptionEditor from '$lib/entry-editor/field-editors/MultiOptionEditor.svelte';
  import {Button} from 'svelte-ux';
  import MapBind from '$lib/utils/MapBind.svelte';

  const fields = {
    'multi1': {show: true, order: 1},
    'multi2': {show: true, order: 2},
    'multi3': {show: true, order: 3},
  };
  const fieldGridAreas = Object.keys(fields).map(field => `'${field}'`).join(' ');

  const alternateView = {
    id: 'alternate',
    i18nKey: '',
    label: 'Alternate',
    fields,
    get alternateView() {
      return defaultView;
    },
  } as const;

  const defaultView = {
    id: 'sandbox',
    i18nKey: '',
    label: 'Language Forge',
    fields,
    alternateView,
  } as const;

  setContext<Readable<View>>('currentView', readable(defaultView));

  const options = [
    {label: 'One', id: '1'},
    {label: 'Two', id: '2'},
    {label: 'Three', id: '3'},
    {label: 'Four', id: '4'},
  ];

  function findOption(id: string) {
    return options.find(o => o.id === id)!;
  }

  let idValue = ['3'];
  let idObjectValue = [{id: '3'}];
  let optionValue = [{label: 'Three', id: '3'}];
</script>
<MapBind bind:in={idValue} bind:out={idObjectValue} map={(value) => value.map(v => ({id: v}))} unmap={(value => value.map(v => v.id))}/>
<MapBind bind:in={idObjectValue} bind:out={optionValue} map={(value) => value.map(v => findOption(v.id))} unmap={(value => value)} />
<div class="grid gap-2" style:grid-template-areas={fieldGridAreas}>
      <MultiOptionEditor id="multi1" name="String values" bind:value={idValue} valuesAreIds getOptionLabel={(o) => o.label} wsType="analysis" readonly={false} {options} />
      <MultiOptionEditor id="multi2" name="(id: string) values" bind:value={idObjectValue} getValueById={findOption} getOptionLabel={(o) => o.label} wsType="analysis" readonly={false} {options} />
      <MultiOptionEditor id="multi3" name="Option values" bind:value={optionValue} getOptionLabel={(o) => o.label} wsType="analysis" readonly={false} {options} />
    </div>
    <div class="flex flex-col">
      <p>selected: {idValue.join('|')}</p>
      <Button variant="fill" on:click={() => idValue = ['4']}>Select Four only</Button>
    </div>
