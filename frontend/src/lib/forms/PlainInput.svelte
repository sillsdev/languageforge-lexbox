<script lang="ts">
  import { run } from 'svelte/legacy';

  import { randomFormId } from './utils';
  import { makeDebouncer } from '$lib/util/time';
  import { createEventDispatcher } from 'svelte';

  const dispatch = createEventDispatcher<{
    input: string | undefined;
  }>();

  let input: HTMLInputElement = $state()!;

  // Despite the compatibility table, 'new-password' seems to work well in Chrome, Edge & Firefox

  export function clear(): void {
    debouncer.clear();
    input.value = ''; // if we cancel the debounce the input and the component can get out of sync
    undebouncedValue = value = undefined;
  }

  export function focus(): void {
    input.focus();
  }

  interface Props {
    id?: any;
    value?: string | undefined | null;
    type?: 'text' | 'email' | 'password';
    autofocus?: true | undefined;
    readonly?: boolean;
    error?: string | string[] | undefined;
    placeholder?: string;
    // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/autocomplete#browser_compatibility
    autocomplete?: 'new-password' | 'current-password' | 'off' | undefined;
    debounce?: number | boolean;
    debouncing?: boolean;
    undebouncedValue?: string | undefined | null;
    style?: string | undefined;
    keydownHandler?: ((event: KeyboardEvent) => void) | undefined;
  }

  let {
    id = randomFormId(),
    value = $bindable(undefined),
    type = 'text',
    autofocus = undefined,
    readonly = false,
    error = undefined,
    placeholder = '',
    autocomplete = undefined,
    debounce = false,
    debouncing = $bindable(false),
    undebouncedValue = $bindable(),
    style = undefined,
    keydownHandler = undefined,
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
  let debouncer = $derived(makeDebouncer((newValue: string | undefined) => (value = newValue), debounce));
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
