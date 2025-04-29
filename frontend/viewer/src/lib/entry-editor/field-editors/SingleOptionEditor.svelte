<script lang="ts"
  generics="TValue extends string | object | undefined, TOption extends object | TValue">
  import {makeHasHadValueTracker} from '$lib/utils';
  import { createEventDispatcher } from 'svelte';
  import MapBind from '../../utils/MapBind.svelte';

  import type { WritingSystemSelection } from '../../config-types';
  import { useCurrentView } from '$lib/views/view-service';
  import { useWritingSystemService } from '../../writing-system-service.svelte';
  import FieldTitle from '../FieldTitle.svelte';
  import CrdtOptionField from '../inputs/CrdtOptionField.svelte';

  type Id = string | undefined;

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  type $$Props = {
    id: string;
    wsType: WritingSystemSelection;
    name?: string;
    readonly: boolean;
    value: TValue;
    options: TOption[];
    getOptionLabel(option: TOption): string;

    // optional based on configuration
    valueIsId?: true;
    getValueId?: (value: TValue) => Id;
    getValueById?: (id: Id) => TValue;
    getOptionId?: (option: TOption) => string;
  }
  & // mappings we support out of the box
  (
    { value: Id | { id: string }; } |
    { getValueId: (value: TValue) => Id; }
  ) & (
    { options: { id: string }[]; } |
    { getOptionId: (value: TOption) => string; }
  ) & (
    { value: Exclude<TValue, string> } |
    { value: Id; valueIsId: true; } | // we need valueIsId to know what type to return at run time
    { getValueById: (id: Id) => TValue }
  );

  const dispatch = createEventDispatcher<{
    change: { value: TValue };
  }>();

  function onChange(): void {
    // wait for the change to be mapped
    setTimeout(() => dispatch('change', {value}));
  }

  export let id: string;
  export let wsType: WritingSystemSelection;
  export let name: string | undefined = undefined;
  export let readonly: boolean = false;
  export let value: TValue;
  export let options: TOption[];

  export let valueIsId: true | undefined = undefined;
  export let getValueId: (value: TValue) => Id = defaultGetValueId;
  export let getValueById: (id: Id) => TValue = defaultGetValueById;
  export let getOptionId: (option: TOption) => string = defaultGetOptionId;
  export let getOptionLabel: (option: TOption) => string;

  let valueId: Id;
  $: uiOptions = options.map(o => ({ value: getOptionId(o), label: getOptionLabel(o) }));

  function defaultGetValueId(value: TValue): Id {
    if (value === undefined || value === null) return undefined as Id;
    if (typeof value === 'string') return value;
    if (typeof value === 'object' && !!value && 'id' in value) return value.id as Id;
    throw new Error('Default getValueId not implemented for ' + typeof value);
  }

  function defaultGetValueById(id: Id): TValue {
    const option = options.find(o => getOptionId(o) === id);
    if (!option) return undefined as TValue;
    if (valueIsId) return getOptionId(option) as TValue;
    else return option as TValue;
  }

  function defaultGetOptionId(option: TOption): string {
    if (typeof option === 'object' && !!option && 'id' in option) return option.id as string;
    throw new Error('Default getOptionId not implemented for ' + typeof option);
  }

  let currentView = useCurrentView();
  const writingSystemService = useWritingSystemService();

  $: [ws] = writingSystemService.pickWritingSystems(wsType);

  let hasHadValueTracker = makeHasHadValueTracker();
  let hasHadValue = hasHadValueTracker.store;
  $: hasHadValueTracker.pushAndGet(value);
</script>

{#key options}
  <MapBind bind:in={value} bind:out={valueId} map={getValueId} unmap={getValueById} />
{/key}
<div class="single-field field" class:unused={!$hasHadValue} class:hidden={!$currentView.fields[id].show} style:grid-area={id}>
  <FieldTitle {id} {name}/>
  <div class="fields">
    <CrdtOptionField on:change={onChange} bind:value={valueId} options={uiOptions} placeholder={ws?.abbreviation} {readonly} />
  </div>
</div>
