<script lang="ts">
  import FieldTitle from './FieldTitle.svelte';
  import type { WritingSystems } from '../mini-lcm';
  import type { Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../utils';
  import type {ViewConfig, WritingSystemSelection} from '../config-types';
  import CrdtTextField from './CrdtTextField.svelte';

  type T = $$Generic<{}>;
  export let id: string;
  export let name: string | undefined = undefined;
  export let wsType: WritingSystemSelection;
  export let value: string;

  const allWritingSystems = getContext<Readable<WritingSystems>>('writingSystems');
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');

  $: [ws] = pickWritingSystems(wsType, $allWritingSystems);
  $: empty = !value;
</script>

<div class="single-field field" class:empty>
  <FieldTitle id={id} {name}/>
  <div class="fields">
    <CrdtTextField on:change bind:value placeholder={ws.abbreviation} readonly={$viewConfig.readonly} />
  </div>
</div>
