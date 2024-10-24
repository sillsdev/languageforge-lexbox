<script lang="ts">
  import type { CustomFieldConfig } from '../../config-types';
  import FieldEditor from '../field-editors/FieldEditor.svelte';

  export let readonly: boolean;
  export let customFieldConfigs: CustomFieldConfig[];
</script>

{#each customFieldConfigs as fieldConfig}
  <FieldEditor on:change value={{values: {}}} field={fieldConfig} {readonly} />
{/each}
