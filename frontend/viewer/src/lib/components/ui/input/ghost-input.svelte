<script lang="ts" generics="T extends string | number">
  import { cn } from '$lib/utils';
  import type { InputProps } from './input.svelte';

  let {
    ref = $bindable(null),
    value = $bindable(),
    class: className,
    ...restProps
  }: Omit<InputProps, 'value'> & { value?: T } = $props();
</script>

<input bind:this={ref} class={cn('outline-none min-w-0 bg-transparent', className)} bind:value {...restProps} />
