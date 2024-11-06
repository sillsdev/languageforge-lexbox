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

  type TValue = $$Generic;
  type TOption = $$Generic;

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  type $$Props = {
    id: string;
    wsType: WritingSystemSelection;
    name?: string;
    readonly: boolean;
    value: TValue[];
    options: TOption[];
    getOptionLabel(option: TOption): string;
    /**
     * Indicates whether the order of the (selection of) values should be preserved, because the order is meaningful.
     */
    preserveOrder?: boolean;

    // optional based on configuration
    valuesAreIds?: true;
    getValueId?: (value: TValue) => string;
    getValueById?: (id: string) => TValue | undefined;
    getOptionId?: (option: TOption) => string;
  }
  & // mappings we support out of the box
  (
    { value: string[] | { id: string }[]; } |
    { getValueId: (value: TValue) => string; }
  ) & (
    { options: { id: string }[]; } |
    { getOptionId: (value: TOption) => string; }
  ) & (
    { value: (TValue & TOption)[] } |
    { value: string[]; valuesAreIds: true; } | // we need valuesAreIds to know what type to return at run time
    { getValueById: (id: string) => TValue | undefined }
  );

  const dispatch = createEventDispatcher<{
    change: { value: TValue[] };
  }>();

  function onChange(): void {
    // wait for the change to be mapped
    setTimeout(() => dispatch('change', {value}));
  }

  export let id: string;
  export let wsType: WritingSystemSelection;
  export let name: string | undefined = undefined;
  export let readonly: boolean = false;
  export let value: TValue[];
  export let options: TOption[];
  export let preserveOrder = false;

  export let valuesAreIds: true | undefined = undefined;
  export let getValueId: (value: TValue) => string = defaultGetValueId;
  export let getValueById: (id: string) => TValue | undefined = defaultGetValueById;
  export let getOptionId: (option: TOption) => string = defaultGetOptionId;
  export let getOptionLabel: (option: TOption) => string;

  let ids: string[] = [];
  $: uiOptions = options.map(o => ({ value: getOptionId(o), label: getOptionLabel(o) }));

  function defaultGetValueId(value: TValue): string {
    if (typeof value === 'string') return value;
    if (typeof value === 'object' && !!value && 'id' in value) return value.id as string;
    throw new Error('Default getValueId not implemented for ' + typeof value);
  }

  function defaultGetValueById(id: string): TValue | undefined {
    const option = options.find(o => getOptionId(o) === id);
    if (!option) return undefined;
    if (valuesAreIds) return getOptionId(option) as TValue;
    else return option as TValue;
  }

  function defaultGetOptionId(option: TOption): string {
    if (typeof option === 'object' && !!option && 'id' in option) return option.id as string;
    throw new Error('Default getOptionId not implemented for ' + typeof option);
  }

  function toIds(value: TValue[]): string[] {
    return value.map(v => getValueId(v));
  }

  function fromIds(uiValue: string[]): TValue[] {
    return uiValue.map(getValueById)
      .filter((value): value is TValue => !!value);
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
    <CrdtMultiOptionField on:change={onChange} bind:value={ids} options={uiOptions} placeholder={ws.abbreviation} {readonly} {preserveOrder} />
  </div>
</div>
