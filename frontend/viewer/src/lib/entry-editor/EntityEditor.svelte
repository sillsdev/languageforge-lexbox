<script lang="ts">
  import { getContext } from 'svelte';
  import type { Readable } from 'svelte/store';
  import type { CustomFieldConfig, EntityFieldConfig, BaseEntityFieldConfig, ViewConfigFieldProps } from '../types';
  import FieldEditor from './FieldEditor.svelte';

  type T = $$Generic<unknown>;
  type FC = $$Generic<EntityFieldConfig & BaseEntityFieldConfig<T> & ViewConfigFieldProps>;
  export let entity: T;
  export let fieldConfigs: FC[];
  export let customFieldConfigs: CustomFieldConfig[];

  const demoValues = getContext<Readable<any>>('demoValues');
</script>

{#each fieldConfigs as fieldConfig}
  {#if !fieldConfig.extra || $demoValues.showExtraFields}
    <FieldEditor on:change value={entity[fieldConfig.id]} field={fieldConfig} />
  {/if}
{/each}

{#each customFieldConfigs as fieldConfig}
  <FieldEditor on:change value={{values: {}}} field={fieldConfig} />
{/each}
