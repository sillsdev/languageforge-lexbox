<script lang="ts">
  import type { MenuOption } from 'svelte-ux';
  import CrdtMultiOptionField from './CrdtMultiOptionField.svelte';
  import FieldTitle from './FieldTitle.svelte';
  import type { WritingSystems } from '../mini-lcm';
  import { type Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../utils';
  import type {
    OptionFieldValue,
    ViewConfig,
    WritingSystemSelection
  } from '../config-types';

  type T = $$Generic<{}>;
  export let id: string;
  export let wsType: WritingSystemSelection;
  export let name: string | undefined = undefined;
  export let value: OptionFieldValue[];

  export let options: MenuOption[] = [];

  const allWritingSystems = getContext<Readable<WritingSystems>>('writingSystems');
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');

  $: [ws] = pickWritingSystems(wsType, $allWritingSystems);
  $: empty = !value?.length;
</script>

<div class="single-field field" class:empty>
  <FieldTitle {id} {name}/>
  <div class="fields">
    <CrdtMultiOptionField on:change bind:value {options} placeholder={ws.abbreviation} readonly={$viewConfig.readonly} />
  </div>
</div>
