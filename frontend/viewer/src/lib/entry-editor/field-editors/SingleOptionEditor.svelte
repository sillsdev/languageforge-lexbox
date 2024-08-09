<script lang="ts">
  import CrdtOptionField from '../inputs/CrdtOptionField.svelte';
  import FieldTitle from '../FieldTitle.svelte';
  import type { WritingSystems } from '../../mini-lcm';
  import { type Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../../utils';
  import type {WritingSystemSelection} from '../../config-types';
  import type { MenuOption } from 'svelte-ux';
  import {useCurrentView} from '../../services/view-service';
  import {useWritingSystems} from '../../writing-systems';

  type T = $$Generic<{}>;
  export let id: string;
  export let wsType: WritingSystemSelection;
  export let name: string | undefined = undefined;
  export let value: string | undefined;
  export let readonly: boolean = false;

  export let options: MenuOption[] = [];
  let currentView = useCurrentView();

  const allWritingSystems = useWritingSystems();

  $: [ws] = pickWritingSystems(wsType, $allWritingSystems);
  $: empty = !value;
</script>

<div class="single-field field" class:empty class:hidden={!$currentView.fields[id].show}>
  <FieldTitle {id} {name}/>
  <div class="fields">
    <CrdtOptionField on:change bind:value {options} placeholder={ws.abbreviation} {readonly} />
  </div>
</div>
