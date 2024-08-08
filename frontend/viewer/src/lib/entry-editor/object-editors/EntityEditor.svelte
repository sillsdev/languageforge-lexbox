<script lang="ts">
  import { getContext } from 'svelte';
  import type { Readable } from 'svelte/store';
  import type { CustomFieldConfig, EntityFieldConfig, BaseEntityFieldConfig, ViewConfig } from '../../config-types';
  import FieldEditor from '../field-editors/FieldEditor.svelte';

  type T = $$Generic<unknown>;
  type FC = $$Generic<EntityFieldConfig & BaseEntityFieldConfig<T>>;
  export let entity: T;
  export let readonly: boolean;
  export let customFieldConfigs: CustomFieldConfig[];

  const viewConfig = getContext<Readable<ViewConfig>>('viewConfig');
</script>


{#each customFieldConfigs as fieldConfig}
  <FieldEditor on:change value={{values: {}}} field={fieldConfig} {readonly} />
{/each}
