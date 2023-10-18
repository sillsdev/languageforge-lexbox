<script lang="ts">
  import { randomFieldId } from './utils';
  import FormField from './FormField.svelte';
  import { debounce as _debounce } from '$lib/util/time';

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
  export let autocomplete: 'new-password' | undefined = undefined;
  export let debounce: number | boolean = false;

  const DEFAULT_DEBOUNCE_TIME = 400;
  $: debounceTime = typeof debounce === 'number' ? debounce : DEFAULT_DEBOUNCE_TIME;
  $: valueSetter = debounce
    ? _debounce((newValue: string) => value = newValue, debounceTime)
    : (newValue: string) => value = newValue;

  function onInput(event: Event): void {
    const newValue = (event.target as HTMLInputElement).value;
    valueSetter(newValue);
  }
</script>

<!-- https://daisyui.com/components/input -->
<FormField {id} {error} {label} {autofocus} {description}>
  <!-- svelte-ignore a11y-autofocus -->
  <input
    {id}
    {type}
    {value}
    class:input-error={error}
    on:input={onInput}
    {placeholder}
    class="input input-bordered"
    {readonly}
    {autofocus}
    {autocomplete}
  />
</FormField>
