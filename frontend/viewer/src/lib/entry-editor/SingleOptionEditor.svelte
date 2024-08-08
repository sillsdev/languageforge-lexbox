<script lang="ts">
  import CrdtOptionField from './CrdtOptionField.svelte';
  import FieldTitle from './FieldTitle.svelte';
  import type { WritingSystems } from '../mini-lcm';
  import { type Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../utils';
  import type {ViewConfig, WritingSystemSelection} from '../config-types';
  import type { MenuOption } from 'svelte-ux';

  type T = $$Generic<{}>;
  export let id: string;
  export let wsType: WritingSystemSelection;
  export let name: string | undefined = undefined;
  export let value: string | undefined;

  export let options: MenuOption[] = [];

  const allWritingSystems = getContext<Readable<WritingSystems>>('writingSystems');
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');

  $: [ws] = pickWritingSystems(wsType, $allWritingSystems);
  $: empty = !value;
</script>

<div class="single-field field" class:empty>
  <FieldTitle {id} {name}/>
  <div class="fields">
    <CrdtOptionField on:change bind:value {options} placeholder={ws.abbreviation} readonly={$viewConfig.readonly} />
  </div>
</div>
