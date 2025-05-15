<script lang="ts">
  import { DEFAULT_DEBOUNCE_TIME } from '$lib/util/time';
  import { Debounced, watch } from 'runed';
  import PlainInput, { type PlainInputProps } from './PlainInput.svelte';

  let input: PlainInput | undefined = $state();

  export function clear(): void {
    debouncer.setImmediately(null);
    input?.clear();
  }

  export function focus(): void {
    input?.focus();
  }

  export function setValue(newValue: string | undefined | null): void {
    newValue ??= null;
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
    undebouncedValue = $bindable<string | null>(value),
    ...rest
  }: Props = $props();

  watch.pre(() => value, setValue);

  const debounceTime: number = $derived(
    typeof debounce === 'boolean' ? (debounce ? DEFAULT_DEBOUNCE_TIME : 0) : debounce,
  );

  const debouncer = new Debounced(
    () => undebouncedValue,
    () => debounceTime,
  );
  $effect.pre(() => {
    if (debounceTime)
      undebouncedValue = value = debouncer.current;
    else
      value = undebouncedValue;
  });
</script>

<PlainInput bind:this={input} bind:value={undebouncedValue} {...rest} />
