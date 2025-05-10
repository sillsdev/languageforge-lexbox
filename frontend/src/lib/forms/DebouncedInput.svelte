<script lang="ts">
  import { DEFAULT_DEBOUNCE_TIME } from '$lib/util/time';
  import { Debounced } from 'runed';
  import PlainInput, { type PlainInputProps } from './PlainInput.svelte';

  let input: PlainInput | undefined = $state();

  export function clear(): void {
    debouncer.setImmediately(undefined);
    input?.clear();
  }

  export function focus(): void {
    input?.focus();
  }

  export function setValue(newValue: string | undefined | null): void {
    debouncer.setImmediately(newValue);
    inputValue = newValue;
  }

  interface Props extends PlainInputProps {
    debounce?: number | boolean;
    debouncing?: boolean;
    undebouncedValue?: string | null;
  }

  let {
    value = $bindable(),
    debounce = true,
    debouncing = $bindable(false),
    undebouncedValue = $bindable(),
    ...rest
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

<PlainInput bind:this={input} bind:value={inputValue} {...rest} />
