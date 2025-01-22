<script lang="ts">
  import FieldTitle from '../FieldTitle.svelte';
  import type {WritingSystemSelection} from '../../config-types';
  import CrdtTextField from '../inputs/CrdtTextField.svelte';
  import {useCurrentView} from '../../services/view-service';
  import {useWritingSystemService} from '../../writing-system-service';

  export let id: string;
  export let name: string | undefined = undefined;
  export let wsType: WritingSystemSelection;
  export let value: string | undefined;
  export let readonly: boolean;
  let currentView = useCurrentView();

  const writingSystemService = useWritingSystemService();

  $: [ws] = writingSystemService.pickWritingSystems(wsType);
  $: empty = !value;
</script>

<div class="single-field field" class:empty class:hidden={!$currentView.fields[id].show} style:grid-area={id}>
  <FieldTitle id={id} {name}/>
  <div class="fields">
    <CrdtTextField on:change bind:value placeholder={ws.abbreviation} {readonly} />
  </div>
</div>
