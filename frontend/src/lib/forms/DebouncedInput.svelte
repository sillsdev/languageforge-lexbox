<script lang="ts">
  import { DEFAULT_DEBOUNCE_TIME } from '$lib/util/time';
  import { Debounced, watch } from 'runed';
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
    value = undebouncedValue = newValue;
  }

  interface Props extends PlainInputProps {
    debounce?: number | boolean;
    undebouncedValue?: string | null;
  }

  let {
    value = $bindable(),
    debounce = true,
    undebouncedValue = $bindable(),
    ...rest
  }: Props = $props();

  watch(() => value, () => {
    undebouncedValue = value;
  });

  const debounceTime: number = $derived(
    typeof debounce === 'boolean' ? (debounce ? DEFAULT_DEBOUNCE_TIME : 0) : debounce,
  );

  const debouncer = new Debounced(
    () => undebouncedValue,
    () => debounceTime,
  );
  watch(() => debouncer.current, () => {
    value = debouncer.current;
  });
</script>

<PlainInput bind:this={input} bind:value={undebouncedValue} {...rest} />
