<script lang="ts">
  import { randomFieldId } from './utils';
  import FormField from './FormField.svelte';

  export let id = randomFieldId();
  export let label: string;
  export let value: string | undefined = undefined;
  export let type = 'text';
  export let autofocus = false;
  export let readonly = false;
  export let error: string | string[] | undefined = undefined;
  export let placeholder = '';

  // works around "svelte(invalid-type)" warning, i.e., can't have a dynamic type AND bind:value...keep an eye on https://github.com/sveltejs/svelte/issues/3921
  function typeWorkaround(node: HTMLInputElement): void {
    node.type = type;
  }
</script>

<!-- https://daisyui.com/components/input -->
<FormField {id} {error} {label} {autofocus}>
  <!-- svelte-ignore a11y-autofocus -->
  <input
    {id}
    use:typeWorkaround
    bind:value
    class:input-error={error}
    {placeholder}
    class="input input-bordered"
    {readonly}
    {autofocus}
  />
</FormField>
