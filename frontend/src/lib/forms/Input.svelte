<script lang="ts">
  import { randomFormId } from './utils';
  import PlainInput from './PlainInput.svelte';
  import FormField from './FormField.svelte';
  import IconButton from '$lib/components/IconButton.svelte';

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

  function togglePasswordVisibility(): void {
    if (type == 'password') {
      currentType = (currentType == 'password') ? 'text' : 'password';
    }
  }

</script>

<FormField {id} {error} {label} {autofocus} {description}>
  {#if (type == 'password')}
    <div class="container">
      <PlainInput {id} bind:value on:input type={currentType} {autofocus} {readonly} {error} {placeholder} {autocomplete} style="w-full" />
      <span class="eye"><IconButton variant="btn-ghost" icon={currentType == 'password' ? 'i-mdi-eye' : 'i-mdi-eye-off'} on:click={togglePasswordVisibility} /></span>
    </div>
  {:else}
    <PlainInput {id} bind:value on:input {type} {autofocus} {readonly} {error} {placeholder} {autocomplete} />
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
