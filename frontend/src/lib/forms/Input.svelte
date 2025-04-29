<script lang="ts">
  import { randomFormId } from './utils';
  import PlainInput from './PlainInput.svelte';
  import FormField from './FormField.svelte';
  import IconButton from '$lib/components/IconButton.svelte';

  // Despite the compatibility table, 'new-password' seems to work well in Chrome, Edge & Firefox
  
  interface Props {
    id?: any;
    label: string;
    description?: string | undefined;
    value?: string | undefined | null;
    type?: 'text' | 'email' | 'password';
    autofocus?: true | undefined;
    readonly?: boolean;
    error?: string | string[] | undefined;
    placeholder?: string;
    // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete#browser_compatibility
    autocomplete?: 'new-password' | 'current-password' | undefined;
  }

  let {
    id = randomFormId(),
    label,
    description = undefined,
    value = $bindable(undefined),
    type = 'text',
    autofocus = undefined,
    readonly = false,
    error = undefined,
    placeholder = '',
    autocomplete = undefined
  }: Props = $props();

  let currentType = $state(type);

  function togglePasswordVisibility(): void {
    if (type == 'password') {
      currentType = (currentType == 'password') ? 'text' : 'password';
    }
  }

</script>

<FormField {id} {error} {label} {autofocus} {description}>
  {#if (type == 'password')}
    <div class="relative">
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
</style>
