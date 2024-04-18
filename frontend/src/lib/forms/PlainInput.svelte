<script lang="ts">
  import { randomFormId } from './utils';
  import { makeDebouncer } from '$lib/util/time';
  import { createEventDispatcher } from 'svelte';

  const dispatch = createEventDispatcher<{
    input: string | undefined;
  }>();

  let input: HTMLInputElement;

  export let id = randomFormId();
  export let value: string | undefined | null = undefined;
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
  export let undebouncedValue: string | undefined | null = undefined;
  export let style: string | undefined = undefined;

  $: undebouncedValue = value;
  $: dispatch('input', value);

  export function clear(): void {
    debouncer.clear();
    input.value = ''; // if we cancel the debounce the input and the component can get out of sync
    undebouncedValue = value = undefined;
  }

  export function focus(): void {
    input.focus();
  }

  $: debouncer = makeDebouncer((newValue: string | undefined) => (value = newValue), debounce);
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
  bind:this={input}
  {id}
  {type}
  {value}
  class:input-error={error && error.length}
  on:input={onInput}
  {placeholder}
  class="input input-bordered {style ?? ''}"
  {readonly}
  {autofocus}
  {autocomplete}
/>
