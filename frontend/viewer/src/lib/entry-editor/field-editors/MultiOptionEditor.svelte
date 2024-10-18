<script lang="ts">
  import type { MenuOption } from 'svelte-ux';
  import CrdtMultiOptionField from '../inputs/CrdtMultiOptionField.svelte';
  import FieldTitle from '../FieldTitle.svelte';
  import { pickWritingSystems } from '../../utils';
  import type { WritingSystemSelection } from '../../config-types';
  import {useCurrentView} from '../../services/view-service';
  import {useWritingSystems} from '../../writing-systems';

  export let id: string;
  export let wsType: WritingSystemSelection;
  export let name: string | undefined = undefined;
  export let readonly: boolean = false;
  export let value: string[];
  export let options: MenuOption<string>[];

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
