<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';
  import {inputVariants} from './input.svelte';

  let {
    ref = $bindable(null),
    class: className,
    children,
    ...restProps
  }: WithElementRef<HTMLAttributes<HTMLDivElement>> = $props();
</script>

<div bind:this={ref} class={cn(inputVariants({variant: 'shell'}), className)} {...restProps}>
  {@render children?.()}
</div>
