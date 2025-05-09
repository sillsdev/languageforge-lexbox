<script lang="ts">
  import { randomFormId } from './utils';
  import { DEFAULT_DEBOUNCE_TIME } from '$lib/util/time';
  import { Debounced } from 'runed';

  let input: HTMLInputElement | undefined = $state();

  export function clear(): void {
    debouncer.setImmediately(undefined);
    inputValue = undefined;
  }

  export function focus(): void {
    input?.focus();
  }

  export function setValue(newValue: string | undefined | null): void {
    debouncer.setImmediately(newValue);
    inputValue = newValue;
  }

  interface Props {
    id?: string;
    value?: string | null;
    onInput?: (value: string | null | undefined) => void;
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
    onInput,
    type = 'text',
    autofocus,
    readonly = false,
    error,
    placeholder = '',
    autocomplete,
    debounce = false,
    debouncing = $bindable(false),
    undebouncedValue = $bindable(),
    style,
    keydownHandler,
  }: Props = $props();

  const debounceTime: number = $derived(
    typeof debounce === 'boolean' ? (debounce ? DEFAULT_DEBOUNCE_TIME : 0) : debounce,
  );

  let inputValue = $state(value);
  let debouncer = new Debounced(
    () => inputValue,
    () => debounceTime,
  );
  $effect(() => {
    value = debouncer.current;
    debouncing = false;
  });
  $effect(() => {
    undebouncedValue = inputValue;
    // The !! part below is so that undefined/null and '' will be considered equal for this purpose
    debouncing = (!!inputValue || !!value) && inputValue != value;
  });
</script>

<!-- https://daisyui.com/components/input -->
<!-- svelte-ignore a11y_autofocus -->
<input
  bind:this={input}
  {id}
  {type}
  bind:value={inputValue}
  oninput={() => onInput?.(inputValue)}
  class:input-error={error && error.length}
  {placeholder}
  class="input input-bordered {style ?? ''}"
  {readonly}
  {autofocus}
  {autocomplete}
  onkeydown={keydownHandler}
/>
