<script lang="ts">
  /* eslint-disable @typescript-eslint/no-duplicate-type-constituents, @typescript-eslint/no-redundant-type-constituents */
  import { createEventDispatcher } from 'svelte';
  import MapBind from '../../utils/MapBind.svelte';

  import type { WritingSystemSelection } from '../../config-types';
  import { useCurrentView } from '../../services/view-service';
  import { pickWritingSystems } from '../../utils';
  import { useWritingSystems } from '../../writing-systems';
  import FieldTitle from '../FieldTitle.svelte';
  import CrdtOptionField from '../inputs/CrdtOptionField.svelte';

  type Value = $$Generic;
  type Option = $$Generic;
  type Id = Value extends undefined ? string | undefined : string;

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  type $$Props = {
    id: string;
    wsType: WritingSystemSelection;
    name?: string;
    readonly: boolean;
    value: Value;
    options: Option[];
    getOptionLabel(option: Option): string;
  }
  & // mappings we support out of the box
  (
    { value: Id | { id: string }; } |
    { getValueId: (value: Value) => Id; }
  ) & (
    { options: { id: string }[]; } |
    { getOptionId: (value: Option) => string; }
  ) & (
    { value: Value & Option } |
    { value: Id; idValue: true; } | // we need idValue or else we don't know what type to return at run time
    { getValueById: (id: Id) => Value }
  );

  const dispatch = createEventDispatcher<{
    change: { value: Value };
  }>();

  function onChange(): void {
    // wait for the change to be mapped
    setTimeout(() => dispatch('change', {value}));
  }

  export let id: string;
  export let wsType: WritingSystemSelection;
  export let name: string | undefined = undefined;
  export let readonly: boolean = false;
  export let value: Value;
  export let options: Option[];

  export let idValue: true | undefined = undefined;
  export let getValueId: (value: Value) => Id = defaultGetValueId;
  export let getValueById: (id: Id) => Value = defaultGetValueById;
  export let getOptionId: (option: Option) => string = defaultGetOptionId;
  export let getOptionLabel: (option: Option) => string;

  let valueId: Id;
  $: uiOptions = options.map(o => ({ value: getOptionId(o), label: getOptionLabel(o) }));

  function defaultGetValueId(value: Value): Id {
    if (value === undefined || value === null) return undefined as Id;
    if (typeof value === 'string') return value as Id;
    if (typeof value === 'object' && !!value && 'id' in value) return value.id as Id;
    throw new Error('Default getValueId not implemented for ' + typeof value);
  }

  function defaultGetValueById(id: Id): Value {
    const option = options.find(o => getOptionId(o) === id);
    if (!option) return undefined as Value;
    if (idValue) return getOptionId(option) as Value;
    else return option as Value;
  }

  function defaultGetOptionId(option: Option): string {
    if (typeof option === 'object' && !!option && 'id' in option) return option.id as string;
    throw new Error('Default getOptionId not implemented for ' + typeof option);
  }

  let currentView = useCurrentView();
  const allWritingSystems = useWritingSystems();

  $: [ws] = pickWritingSystems(wsType, $allWritingSystems);
  $: empty = !value;
</script>

<MapBind bind:in={value} bind:out={valueId} map={getValueId} unmap={getValueById} />
<div class="single-field field" class:empty class:hidden={!$currentView.fields[id].show} style:grid-area={id}>
  <FieldTitle {id} {name}/>
  <div class="fields">
    <CrdtOptionField on:change={onChange} bind:value={valueId} options={uiOptions} placeholder={ws.abbreviation} {readonly} />
  </div>
</div>
