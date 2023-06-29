<script lang="ts">
  import { randomFieldId } from './utils';
  import FormField from './FormField.svelte';

  export let label: string;
  export let value = '';
  export let id = randomFieldId();
  export let autofocus = false;
  export let readonly = false;
  export let error: string | string[] | undefined = undefined;
  export let placeholder = '';
</script>

<!-- https://daisyui.com/components/input -->
<FormField {id} {error} {label} {autofocus}>
  <textarea
    {id}
    bind:value
    class="textarea textarea-bordered h-24"
    class:textarea-error={error}
    {placeholder}
    {readonly}
  />
</FormField>
