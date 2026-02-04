<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';
  import {inputVariants} from './input.svelte';

  interface Props extends WithElementRef<HTMLAttributes<HTMLDivElement>> {
    focusRingClass?: string;
  }

  const anyChildHasFocusRing = 'ring-ring outline-none ring-offset-2 has-[:focus-visible]:ring-2';

  let {
    ref = $bindable(null),
    class: className,
    focusRingClass = anyChildHasFocusRing,
    children,
    ...restProps
  }: Props = $props();
</script>

<div bind:this={ref} class={cn(inputVariants({variant: 'shell'}), focusRingClass, className)} {...restProps}>
  {@render children?.()}
</div>
