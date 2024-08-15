<script lang="ts">
  import FieldTitle from '../FieldTitle.svelte';
  import type { WritingSystems } from '../../mini-lcm';
  import type { Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../../utils';
  import type {WritingSystemSelection} from '../../config-types';
  import CrdtTextField from '../inputs/CrdtTextField.svelte';
  import {useCurrentView} from '../../services/view-service';
  import {useWritingSystems} from '../../writing-systems';

  type T = $$Generic<{}>;
  export let id: string;
  export let name: string | undefined = undefined;
  export let wsType: WritingSystemSelection;
  export let value: string;
  export let readonly: boolean;
  let currentView = useCurrentView();

  const allWritingSystems = useWritingSystems();

  $: [ws] = pickWritingSystems(wsType, $allWritingSystems);
  $: empty = !value;
</script>

<div class="single-field field" class:empty class:hidden={!$currentView.fields[id].show} style:grid-area={id}>
  <FieldTitle id={id} {name}/>
  <div class="fields">
    <CrdtTextField on:change bind:value placeholder={ws.abbreviation} {readonly} />
  </div>
</div>
