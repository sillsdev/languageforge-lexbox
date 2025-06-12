<script lang="ts">
  import type { CustomFieldConfig } from '../../config-types';
  import FieldEditor from '../field-editors/FieldEditor.svelte';

  export let customFieldConfigs: CustomFieldConfig[];
</script>

{#each customFieldConfigs as fieldConfig}
  <FieldEditor value={{values: {}}} field={fieldConfig} />
{/each}
