<script lang="ts">
  import type { CustomFieldConfig, EntityFieldConfig, BaseEntityFieldConfig } from '../../config-types';
  import FieldEditor from '../field-editors/FieldEditor.svelte';

  type T = $$Generic<unknown>;
  type FC = $$Generic<EntityFieldConfig & BaseEntityFieldConfig<T>>;
  export let entity: T;
  export let readonly: boolean;
  export let customFieldConfigs: CustomFieldConfig[];
</script>


{#each customFieldConfigs as fieldConfig}
  <FieldEditor on:change value={{values: {}}} field={fieldConfig} {readonly} />
{/each}
