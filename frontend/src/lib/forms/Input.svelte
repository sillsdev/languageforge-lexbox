<script lang="ts">
  import { randomFieldId } from './utils';
  import { debounce as _debounce } from '$lib/util/time';

  export let id = randomFieldId();
  export let value: string | undefined = undefined;
  export let type: 'text' | 'email' | 'password' = 'text';
  export let autofocus: true | undefined = undefined;
  export let readonly = false;
  export let error: string | string[] | undefined = undefined;
  export let placeholder = '';
  // Despite the compatibility table, 'new-password' seems to work well in Chrome, Edge & Firefox
  // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete#browser_compatibility
  export let autocomplete: 'new-password' | 'current-password' | undefined = undefined;
  export let debounce: number | boolean = false;
  export let debouncing = false;
  export let undebouncedValue: string | undefined = undefined;
  $: undebouncedValue = value;
  export let style: string | undefined = undefined;

  $: debouncer = _debounce((newValue: string | undefined) => (value = newValue), debounce);
  $: debouncingStore = debouncer.debouncing;
  $: debouncing = $debouncingStore;

  function onInput(event: Event): void {
    const currValue = (event.target as HTMLInputElement).value;
    undebouncedValue = currValue;
    debouncer.debounce(currValue);
  }
</script>

<!-- https://daisyui.com/components/input -->
<!-- svelte-ignore a11y-autofocus -->
<input
  {id}
  {type}
  {value}
  class:input-error={error}
  on:input={onInput}
  {placeholder}
  class="input input-bordered {style ?? ''}"
  {readonly}
  {autofocus}
  {autocomplete}
/>
