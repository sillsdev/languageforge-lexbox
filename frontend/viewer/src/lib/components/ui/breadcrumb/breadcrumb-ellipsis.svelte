<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithElementRef, WithoutChildren} from 'bits-ui';
  import Ellipsis from 'lucide-svelte/icons/ellipsis';
  import type {HTMLAttributes} from 'svelte/elements';

  let {
    ref = $bindable(null),
    class: className,
    ...restProps
  }: WithoutChildren<WithElementRef<HTMLAttributes<HTMLSpanElement>>> = $props();
</script>

<span
  bind:this={ref}
  role="presentation"
  aria-hidden="true"
  class={cn('flex size-9 items-center justify-center', className)}
  {...restProps}
>
  <Ellipsis class="size-4" />
  <span class="sr-only">More</span>
</span>
