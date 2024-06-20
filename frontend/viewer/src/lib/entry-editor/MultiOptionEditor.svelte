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
  import type { FieldConfig, OptionFieldConfig, ViewConfig } from '../config-types';

  type T = $$Generic<{}>;
  export let field: FieldConfig & OptionFieldConfig;
  export let value: string[];

  let options: Readable<MenuOption[]> = readable([]);
  $: options = pickOptions(field);

  const optionProvider = getContext<OptionProvider>('optionProvider');

  function pickOptions(field: FieldConfig & OptionFieldConfig): Readable<MenuOption[]> {
    switch (field.optionType as WellKnownMultiOptionType) {
      case 'semantic-domain':
        return optionProvider.semanticDomains;
      default:
        throw new Error(`No options for multi-option field ${field.id} (Option type: ${field.optionType})`);
    }
  }

  const allWritingSystems = getContext<Readable<WritingSystems>>('writingSystems');
  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');

  $: [ws] = pickWritingSystems(field.ws, $allWritingSystems);
  $: empty = !value?.length;
</script>

<div class="single-field field" class:empty class:extra={'extra' in field && field.extra}>
  <FieldTitle {field} />
  <div class="fields">
    <CrdtMultiOptionField on:change bind:value options={$options} placeholder={ws.abbreviation} readonly={field.readonly || $viewConfig.readonly} />
  </div>
</div>
