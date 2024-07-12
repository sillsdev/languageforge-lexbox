<script lang="ts">
  import type { WellKnownSingleOptionType } from '../config-data';

  import type { OptionProvider } from '../services/option-provider';

  import CrdtOptionField from './CrdtOptionField.svelte';

  import FieldTitle from './FieldTitle.svelte';
  import type { WritingSystems } from '../mini-lcm';
  import { readable, type Readable } from 'svelte/store';
  import { getContext } from 'svelte';
  import { pickWritingSystems } from '../utils';
  import type { FieldConfig, OptionFieldConfig, ViewConfig } from '../config-types';
  import type { MenuOption } from 'svelte-ux';

  type T = $$Generic<{}>;
  export let field: FieldConfig & OptionFieldConfig;
  export let value: string | undefined;

  let options: Readable<MenuOption[]> = readable([]);
  $: options = pickOptions(field);

  const optionProvider = getContext<OptionProvider>('optionProvider');

  function pickOptions(field: FieldConfig & OptionFieldConfig): Readable<MenuOption[]> {
    switch (field.optionType as WellKnownSingleOptionType) {
      case 'part-of-speech':
        return optionProvider.partsOfSpeech;
      default:
        throw new Error(`No options for single-option field ${field.id} (Option type: ${field.optionType})`);
    }
  }

  const allWritingSystems = getContext<Readable<WritingSystems>>('writingSystems');
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');

  $: [ws] = pickWritingSystems(field.ws, $allWritingSystems);
  $: empty = !value;
</script>

<div class="single-field field" class:empty class:extra={'extra' in field && field.extra}>
  <FieldTitle {field} />
  <div class="fields">
    <CrdtOptionField on:change bind:value options={$options} placeholder={ws.abbreviation} readonly={field.readonly || $viewConfig.readonly} />
  </div>
</div>
