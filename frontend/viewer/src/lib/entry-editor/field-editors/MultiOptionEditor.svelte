<script lang="ts">
  import type { MenuOption } from 'svelte-ux';
  import CrdtMultiOptionField from '../inputs/CrdtMultiOptionField.svelte';
  import FieldTitle from '../FieldTitle.svelte';
  import type { WritingSystems } from '../../mini-lcm';
  import { type Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../../utils';
  import type {
    OptionFieldValue,
    WritingSystemSelection
  } from '../../config-types';
  import {useCurrentView} from '../../services/view-service';
  import {useWritingSystems} from '../../writing-systems';

  type T = $$Generic<{}>;
  export let id: string;
  export let wsType: WritingSystemSelection;
  export let name: string | undefined = undefined;
  export let readonly: boolean = false;
  export let value: OptionFieldValue[];

  export let options: MenuOption[] = [];
  let currentView = useCurrentView();

  const allWritingSystems = useWritingSystems();

  $: [ws] = pickWritingSystems(wsType, $allWritingSystems);
  $: empty = !value?.length;
</script>

<div class="single-field field" class:empty class:hidden={!$currentView.fields[id].show} style:grid-area={id}>
  <FieldTitle {id} {name}/>
  <div class="fields">
    <CrdtMultiOptionField on:change bind:value {options} placeholder={ws.abbreviation} {readonly} />
  </div>
</div>
