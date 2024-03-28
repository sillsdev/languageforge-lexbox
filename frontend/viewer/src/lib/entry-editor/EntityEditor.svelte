<script lang="ts">
  import type { CustomFieldConfig, EntityFieldConfig, BaseEntityFieldConfig } from '../types';
  import FieldEditor from './FieldEditor.svelte';

  type T = $$Generic<unknown>;
  type FC = $$Generic<EntityFieldConfig & BaseEntityFieldConfig<T>>;
  export let entity: T;
  export let fieldConfigs: FC[];
  export let customFieldConfigs: CustomFieldConfig[];
</script>

{#each fieldConfigs as fieldConfig}
  <FieldEditor on:change value={entity[fieldConfig.id]} field={fieldConfig} />
{/each}

{#each customFieldConfigs as fieldConfig}
  <FieldEditor on:change value={{values: {}}} field={fieldConfig} />
{/each}