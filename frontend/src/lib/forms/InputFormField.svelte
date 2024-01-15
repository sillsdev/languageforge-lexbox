<script lang="ts">
  import { randomFieldId } from './utils';
  import Input from './Input.svelte';
  import FormField from './FormField.svelte';

  export let id = randomFieldId();
  export let label: string;
  export let description: string | undefined = undefined;
  export let value: string | undefined = undefined;
  export let type: 'text' | 'email' | 'password' = 'text';
  export let autofocus: true | undefined = undefined;
  export let readonly = false;
  export let error: string | string[] | undefined = undefined;
  export let placeholder = '';
  // Despite the compatibility table, 'new-password' seems to work well in Chrome, Edge & Firefox
  // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete#browser_compatibility
  export let autocomplete: 'new-password' | 'current-password' | undefined = undefined;
</script>

<!-- https://daisyui.com/components/input -->
<FormField {id} {error} {label} {autofocus} {description}>
  <Input {id} bind:value {type} {autofocus} {readonly} {error} {placeholder} {autocomplete} />
</FormField>
