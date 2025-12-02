<script lang="ts">
  import { randomFormId } from './utils';
  import PlainInput from './PlainInput.svelte';
  import FormField from './FormField.svelte';
  import IconButton from '$lib/components/IconButton.svelte';

  interface Props {
    id?: string;
    label: string;
    description?: string;
    value?: string | null;
    type?: 'text' | 'email' | 'password';
    autofocus?: true;
    readonly?: boolean;
    error?: string | string[];
    placeholder?: string;
    // Despite the compatibility table, 'new-password' seems to work well in Chrome, Edge & Firefox
    // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete#browser_compatibility
    autocomplete?: 'new-password' | 'current-password';
  }

  let {
    id = randomFormId(),
    label,
    description,
    value = $bindable(),
    type = 'text',
    autofocus,
    readonly = false,
    error,
    placeholder = '',
    autocomplete,
  }: Props = $props();

  let currentType = $derived(type);

  function togglePasswordVisibility(): void {
    if (type == 'password') {
      currentType = currentType == 'password' ? 'text' : 'password';
    }
  }
</script>

<FormField {id} {error} {label} {autofocus} {description}>
  {#if type == 'password'}
    <div class="relative">
      <PlainInput
        {id}
        bind:value
        type={currentType}
        {autofocus}
        {readonly}
        {error}
        {placeholder}
        {autocomplete}
        style="w-full"
      />
      <span class="eye"
        ><IconButton
          variant="btn-ghost"
          icon={currentType == 'password' ? 'i-mdi-eye' : 'i-mdi-eye-off'}
          onclick={togglePasswordVisibility}
        /></span
      >
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
</style>
