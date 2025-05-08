<script lang="ts">
  import { run } from 'svelte/legacy';

  import { randomFormId } from './utils';
  import { makeDebouncer } from '$lib/util/time';
  import { createEventDispatcher } from 'svelte';

  const dispatch = createEventDispatcher<{
    input: string | undefined;
  }>();

  let input: HTMLInputElement | undefined = $state();

  export function clear(): void {
    debouncer.clear();
    if (input) input.value = ''; // if we cancel the debounce the input and the component can get out of sync
    undebouncedValue = value = undefined;
  }

  export function focus(): void {
    input?.focus();
  }

  interface Props {
    id?: string;
    value?: string | null;
    type?: 'text' | 'email' | 'password';
    autofocus?: true;
    readonly?: boolean;
    error?: string | string[];
    placeholder?: string;
    // Despite the compatibility table, 'new-password' seems to work well in Chrome, Edge & Firefox
    // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete#browser_compatibility
    autocomplete?: 'new-password' | 'current-password' | 'off';
    debounce?: number | boolean;
    debouncing?: boolean;
    undebouncedValue?: string | null;
    style?: string;
    keydownHandler?: (event: KeyboardEvent) => void;
  }

  let {
    id = randomFormId(),
    value = $bindable(),
    type = 'text',
    autofocus,
    readonly = false,
    error,
    placeholder = '',
    autocomplete,
    debounce = $bindable(false),
    debouncing = $bindable(false),
    undebouncedValue = $bindable(),
    style,
    keydownHandler,
  }: Props = $props();

  let handlingInputEvent = $state(false);
  let handlingInputEventTimeout: ReturnType<typeof setTimeout>;
  function onInput(event: Event): void {
    clearTimeout(handlingInputEventTimeout);
    handlingInputEvent = true;
    const currValue = (event.target as HTMLInputElement).value;
    undebouncedValue = currValue;
    debouncer.debounce(currValue);
    handlingInputEventTimeout = setTimeout(() => (handlingInputEvent = false));
  }
  // TODO: Switch to using a Svelte 5 debouncer like https://runed.dev/docs/utilities/debounced
  let debouncer = makeDebouncer((newValue: string | undefined) => (value = newValue), debounce);
  run(() => {
    undebouncedValue = value;
  });
  run(() => {
    if (handlingInputEvent) dispatch('input', value);
  });
  let debouncingStore = $derived(debouncer.debouncing);
  run(() => {
    debouncing = $debouncingStore;
  });
</script>

<!-- https://daisyui.com/components/input -->
<!-- svelte-ignore a11y_autofocus -->
<input
  bind:this={input}
  {id}
  {type}
  {value}
  class:input-error={error && error.length}
  oninput={onInput}
  {placeholder}
  class="input input-bordered {style ?? ''}"
  {readonly}
  {autofocus}
  {autocomplete}
  onkeydown={keydownHandler}
/>
