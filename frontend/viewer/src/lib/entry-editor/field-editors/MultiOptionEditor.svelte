<script lang="ts">
  /* eslint-disable @typescript-eslint/no-duplicate-type-constituents, @typescript-eslint/no-redundant-type-constituents */
  import {createEventDispatcher} from 'svelte';
  import type {WritingSystemSelection} from '../../config-types';
  import {useCurrentView} from '../../services/view-service';
  import {pickWritingSystems} from '../../utils';
  import MapBind from '../../utils/MapBind.svelte';
  import {useWritingSystems} from '../../writing-systems';
  import FieldTitle from '../FieldTitle.svelte';
  import CrdtMultiOptionField from '../inputs/CrdtMultiOptionField.svelte';

  type Value = $$Generic;
  type Option = $$Generic;

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  type $$Props = {
    id: string;
    wsType: WritingSystemSelection;
    name?: string;
    readonly: boolean;
    value: Value[];
    options: Option[];
    getOptionLabel(option: Option): string;
  }
  & // mappings we support out of the box
  (
    { value: string[] | { id: string }[]; } |
    { getValueId: (value: Value) => string; }
  ) & (
    { options: { id: string }[]; } |
    { getOptionId: (value: Option) => string; }
  ) & (
    { value: (Value & Option)[] } |
    { value: string[]; idValues: true; } | // we need idValue or else we don't know what type to return at run time
    { getValueById: (id: string) => Value | undefined }
  );

  const dispatch = createEventDispatcher<{
    change: { value: Value[] };
  }>();

  function onChange(): void {
    // wait for the change to be mapped
    setTimeout(() => dispatch('change', {value}));
  }

  export let id: string;
  export let wsType: WritingSystemSelection;
  export let name: string | undefined = undefined;
  export let readonly: boolean = false;
  export let value: Value[];
  export let options: Option[];

  export let idValues: true | undefined = undefined;
  export let getValueId: (value: Value) => string = defaultGetValueId;
  export let getValueById: (id: string) => Value | undefined = defaultGetValueById;
  export let getOptionId: (option: Option) => string = defaultGetOptionId;
  export let getOptionLabel: (option: Option) => string;

  let ids: string[] = [];
  $: uiOptions = options.map(o => ({ value: getOptionId(o), label: getOptionLabel(o) }));

  function defaultGetValueId(value: Value): string {
    if (typeof value === 'string') return value;
    if (typeof value === 'object' && !!value && 'id' in value) return value.id as string;
    throw new Error('Default getValueId not implemented for ' + typeof value);
  }

  function defaultGetValueById(id: string): Value | undefined {
    const option = options.find(o => getOptionId(o) === id);
    if (!option) return undefined;
    if (idValues) return getOptionId(option) as Value;
    else return option as Value;
  }

  function defaultGetOptionId(option: Option): string {
    if (typeof option === 'object' && !!option && 'id' in option) return option.id as string;
    throw new Error('Default getOptionId not implemented for ' + typeof option);
  }

  function toIds(value: Value[]): string[] {
    return value.map(v => getValueId(v));
  }

  function fromIds(uiValue: string[]): Value[] {
    return uiValue.map(getValueById)
      .filter((value): value is Value => !!value);
  }

  let currentView = useCurrentView();
  const allWritingSystems = useWritingSystems();

  $: [ws] = pickWritingSystems(wsType, $allWritingSystems);
  $: empty = !value?.length;
</script>

<MapBind bind:in={value} bind:out={ids} map={toIds} unmap={fromIds} />
<div class="single-field field" class:empty class:hidden={!$currentView.fields[id].show} style:grid-area={id}>
  <FieldTitle {id} {name}/>
  <div class="fields">
    <CrdtMultiOptionField on:change={onChange} bind:value={ids} options={uiOptions} placeholder={ws.abbreviation} {readonly} {ordered} />
  </div>
</div>
