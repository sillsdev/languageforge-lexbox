<script lang="ts">
  import type { CustomFieldConfig } from '../../config-types';
  import FieldEditor from '../field-editors/FieldEditor.svelte';

  type T = $$Generic<unknown>;
  // eslint-disable-next-line svelte/valid-compile
  export let entity: T;
  export let readonly: boolean;
  export let customFieldConfigs: CustomFieldConfig[];
</script>

{#each customFieldConfigs as fieldConfig}
  <FieldEditor on:change value={{values: {}}} field={fieldConfig} {readonly} />
{/each}
