<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  let {
    ref = $bindable(null),
    class: className,
    children,
    ...restProps
  }: WithElementRef<HTMLAttributes<HTMLDivElement>> = $props();
</script>

<div
  bind:this={ref}
  class={cn('flex flex-col-reverse gap-2 sm:flex-row sm:justify-end items-end flex-wrap', className)}
  {...restProps}
>
  {@render children?.()}
</div>
