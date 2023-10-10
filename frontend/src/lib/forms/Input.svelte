<script lang="ts">
  import { randomFieldId } from './utils';
  import FormField from './FormField.svelte';

  export let id = randomFieldId();
  export let label: string;
  export let description: string | undefined = undefined;
  export let value: string | undefined = undefined;
  export let type: 'text' | 'email' | 'password' = 'text';
  export let autofocus = false;
  export let readonly = false;
  export let error: string | string[] | undefined = undefined;
  export let placeholder = '';
  // Despite the compatibility table, 'new-password' seems to work well in Chrome, Edge & Firefox
  // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete#browser_compatibility
  export let autocomplete: 'new-password' | undefined = undefined;
</script>

<!-- https://daisyui.com/components/input -->
<FormField {id} {error} {label} {autofocus} {description}>
  <!-- svelte-ignore a11y-autofocus -->
  <!--
    {...{type}} prevents a Svelte compile error (https://github.com/sveltejs/svelte/issues/3921)
    and is perfectly safe, because we limit the type to string values
  -->
  <input
    {id}
    {...{ type }}
    bind:value
    class:input-error={error}
    {placeholder}
    class="input input-bordered"
    {readonly}
    {autofocus}
    {autocomplete}
  />
</FormField>
