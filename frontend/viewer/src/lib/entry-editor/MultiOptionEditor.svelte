<script lang="ts">
  import type { WellKnownMultiOptionType } from '../config-data';

  import type { OptionProvider } from '../services/option-provider';

  import type { MenuOption } from 'svelte-ux';

  import CrdtMultiOptionField from './CrdtMultiOptionField.svelte';

  import FieldTitle from './FieldTitle.svelte';
  import type { WritingSystems } from '../mini-lcm';
  import { readable, type Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../utils';
  import type {FieldConfig, OptionFieldConfig, OptionFieldValue, ViewConfig} from '../config-types';

  type T = $$Generic<{}>;
  export let field: FieldConfig & OptionFieldConfig;
  export let value: OptionFieldValue[];

  export let options: MenuOption[] = [];

  const allWritingSystems = getContext<Readable<WritingSystems>>('writingSystems');
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');

  $: [ws] = pickWritingSystems(field.ws, $allWritingSystems);
  $: empty = !value?.length;
</script>

<div class="single-field field" class:empty class:extra={field.extra}>
  <FieldTitle id={field.id} name={field.name} helpId={field.helpId} extra={field.extra}/>
  <div class="fields">
    <CrdtMultiOptionField on:change bind:value {options} placeholder={ws.abbreviation} readonly={field.readonly || $viewConfig.readonly} />
  </div>
</div>
