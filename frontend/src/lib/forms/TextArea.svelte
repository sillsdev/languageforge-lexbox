<script lang="ts">
  import { randomFormId } from './utils';
  import FormField from './FormField.svelte';

  export let label: string;
  export let value: string | undefined = undefined;
  export let id = randomFormId();
  export let autofocus = false;
  export let readonly = false;
  export let error: string | string[] | undefined = undefined;
  export let placeholder = '';
</script>

<!-- https://daisyui.com/components/input -->
<FormField {id} {error} {label} {autofocus}>
  <!-- svelte-ignore a11y-autofocus -->
  <textarea
    {id}
    bind:value
    class="textarea textarea-bordered h-24"
    class:textarea-error={error}
    {placeholder}
    {readonly}
    {autofocus}
  />
</FormField>
