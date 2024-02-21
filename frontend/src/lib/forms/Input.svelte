<script lang="ts">
  import { randomFormId } from './utils';
  import PlainInput from './PlainInput.svelte';
  import FormField from './FormField.svelte';
  import Icon from '$lib/icons/Icon.svelte';

  export let id = randomFormId();
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

  let currentType = type;
  if (type == 'password') console.log('Password field displaying');

</script>

<FormField {id} {error} {label} {autofocus} {description}>
  {#if (type == 'password')}
  <div class="container">
    <PlainInput {id} bind:value type={currentType} {autofocus} {readonly} {error} {placeholder} {autocomplete} />
    <span class="eye"><Icon icon="i-mdi-eye" size="text-md"></Icon></span>
  </div>
  {:else}
    <PlainInput {id} bind:value {type} {autofocus} {readonly} {error} {placeholder} {autocomplete} />
  {/if}
</FormField>

<style>
  .eye {
    position: absolute;
    right: 1px;
  }
  .container {
    position: relative;
  }
</style>
