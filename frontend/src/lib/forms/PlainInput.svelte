<script lang="ts">
  import { randomFormId } from './utils';

  let input: HTMLInputElement | undefined = $state();

  export function clear(): void {
    value = undefined;
  }

  export function focus(): void {
    input?.focus();
  }

  export interface PlainInputProps {
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
    style?: string;
    onInput?: (value: string | null | undefined) => void;
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
    style,
    onInput,
    keydownHandler,
  }: PlainInputProps = $props();
</script>

<!-- https://daisyui.com/components/input -->
<!-- svelte-ignore a11y_autofocus -->
<input
  bind:this={input}
  {id}
  {type}
  bind:value
  class:input-error={error && error.length}
  {placeholder}
  class="input input-bordered {style ?? ''}"
  {readonly}
  {autofocus}
  {autocomplete}
  oninput={onInput ? () => onInput(value) : undefined}
  onkeydown={keydownHandler}
/>
